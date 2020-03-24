using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Classes;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using static Kaenx.Classes.Bus.Actions.IBusAction;

namespace Kaenx.Classes.Bus.Actions
{
    public class ProgApplication : IBusAction, INotifyPropertyChanged
    {
        private ProgAppType _type;
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private byte _sequence = 0x00;
        private CancellationToken _token;
        private byte _currentSeqNum = 0;
        private string appId;
        private List<string> addedGroups;
        private CatalogContext _context = new CatalogContext();

        public string Type { get; } = "Applikation";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public Connection Connection { get; set; }

        private BusDevice dev;
        public event ActionFinishedHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;

        public ProgApplication(ProgAppType type)
        {
            _type = type;
        }

        public void Run(CancellationToken token)
        {
            _token = token;


            Start3();

            //Start();
        }


        private async void Start3()
        {
            dev = new BusDevice(Device.LineName, Connection);
            TodoText = "Applikation schreiben";

            XElement merges = null;
            XElement loadprocedures = null;
            CatalogContext _context = new CatalogContext();
            ApplicationViewModel app = _context.Applications.Single(a => a.Id == Device.ApplicationId);

            AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == Device.ApplicationId);
            XElement prod = XDocument.Parse(Encoding.UTF8.GetString(adds.LoadProcedures)).Root;
            loadprocedures = prod.Element(XName.Get("LoadProcedure", prod.Name.NamespaceName));

            if(prod.Elements().Any(e => e.Attribute("MergeId") != null))
            {
                merges = prod;
                XDocument master = await GetKnxMaster();
                XElement mask = master.Descendants(XName.Get("MaskVersion", master.Root.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == app.Mask);
                loadprocedures = mask.Descendants(XName.Get("Procedure", master.Root.Name.NamespaceName)).Single(m => m.Attribute("ProcedureType").Value == "Load");
            }


            double stepSize = 100.0 / loadprocedures.Elements().Count();
            double currentProg = 0;
            Debug.WriteLine("StepSize: " + stepSize + " - " + prod.Elements().Count());

            foreach(XElement ctrl in loadprocedures.Elements())
            {
                currentProg += stepSize;
                ProgressValue = (int)currentProg;

                switch (ctrl.Name.LocalName)
                {
                    case "LdCtrlConnect":
                        dev.Connect();
                        await Task.Delay(100);
                        break;

                    case "LdCtrlDisconnect":
                        dev.Disconnect();
                        break;

                    case "LdCtrlRestart":
                        dev.Restart();
                        break;

                    case "LdCtrlCompareProp":
                        int obj = int.Parse(ctrl.Attribute("ObjIdx").Value);
                        int pid = int.Parse(ctrl.Attribute("PropId").Value);
                        byte[] prop = await dev.PropertyRead(Convert.ToByte(obj), Convert.ToByte(pid), 0);
                        string dataCP = ctrl.Attribute("InlineData").Value;

                        if(!dataCP.StartsWith(BitConverter.ToString(prop).Replace("-", "")))
                        {
                            TodoText = "Fehler beim schreiben! PAx01";
                            dev.Disconnect();
                            Finished?.Invoke(this, null);
                            return;
                        }
                        break;

                    case "LdCtrlUnload":
                    case "LdCtrlLoad":
                    case "LdCtrlLoadCompleted":
                        //if (ctrl.Attribute("LsmIdx").Value == "3") continue;
                        await LsmState(ctrl);
                        break;

                    case "LdCtrlTaskSegment":
                        //if (ctrl.Attribute("LsmIdx").Value == "3") continue;
                        await AllocSegment(ctrl, 2);
                        break;

                    case "LdCtrlAbsSegment":
                        //if (ctrl.Attribute("LsmIdx").Value == "3") continue;
                        await WriteAbsSegment(ctrl);
                        break;

                    case "LdCtrlDelay":
                        int ms = int.Parse(ctrl.Attribute("MilliSeconds").Value);
                        await Task.Delay(ms);
                        break;
                }
            }

            TodoText = "Abgeschlossen";
            ProgressValue = 100;

            //Finished?.Invoke(this, null);
        }


        private async Task WriteAbsSegment(XElement ctrl)
        {
            await AllocSegment(ctrl, int.Parse(ctrl.Attribute("SegType").Value));


            if (ctrl.Attribute("Access") != null && ctrl.Attribute("Access").Value == "0") return;
            int addr = int.Parse(ctrl.Attribute("Address").Value);


            switch (ctrl.Attribute("LsmIdx").Value)
            {
                case "1":
                    await WriteTableGroup(addr);
                    break;

                case "2":
                    await WriteAssociationTable(addr);
                    break;

                case "3":
                    //TODO später
                    break;

                case "4":
                    //TODO später
                    break;
            }
        }

        private async Task AllocSegment(XElement ctrl, int segType, int counter = 0)
        {
            string addr = ctrl.Attribute("Address").Value;
            string LsmId = ctrl.Attribute("LsmIdx").Value;

            byte[] tempBytes;
            List<byte> data = new List<byte>();
            int lsmIdx = int.Parse(LsmId);
            lsmIdx = (lsmIdx << 4) | 0x03;
            data.Add(Convert.ToByte(lsmIdx));
            data.Add(Convert.ToByte(segType)); // Segment Type
            data.Add(0x00); // Segment Id

            switch (segType)
            {
                case 0:
                case 1:
                    tempBytes = BitConverter.GetBytes(Convert.ToUInt16(addr));
                    data.AddRange(tempBytes.Reverse()); // Start Address
                    tempBytes = BitConverter.GetBytes(Convert.ToUInt16(ctrl.Attribute("Size").Value));
                    data.AddRange(tempBytes.Reverse()); // Length
                    data.Add(Convert.ToByte(ctrl.Attribute("Access").Value)); // Access Attributes
                    data.Add(Convert.ToByte(ctrl.Attribute("MemType").Value)); // Memory Type
                    data.Add(Convert.ToByte(ctrl.Attribute("SegFlags").Value)); // Memory Attributes
                    data.Add(0x00); // Reserved
                    break;

                case 2:
                    tempBytes = BitConverter.GetBytes(Convert.ToUInt16(addr));
                    data.AddRange(tempBytes.Reverse()); // Start Address
                    data.Add(0x01); //PEI Type //TODO check to find out

                    string[] appid = Device.ApplicationId.Split('-');
                    int version = int.Parse(appid[3], System.Globalization.NumberStyles.HexNumber);
                    int appnr = int.Parse(appid[2], System.Globalization.NumberStyles.HexNumber);
                    int manu = int.Parse(appid[1].Substring(0,4), System.Globalization.NumberStyles.HexNumber);

                    tempBytes = BitConverter.GetBytes(Convert.ToUInt16(manu));
                    data.AddRange(tempBytes.Reverse());
                    tempBytes = BitConverter.GetBytes(Convert.ToUInt16(appnr));
                    data.AddRange(tempBytes.Reverse());
                    data.Add(Convert.ToByte(version));
                    break;

                default:
                    Debug.WriteLine("Unsupported SegType: " + ctrl.Attribute("SegType").Value);
                    Log.Error("Unsupported SegType: " + ctrl.Attribute("SegType").Value);
                    break;
            }

            await dev.MemoryWriteSync(260, data.ToArray());

            byte[] data2 = await dev.MemoryRead(46825 + int.Parse(LsmId), 1);

            Dictionary<int, byte> map = new Dictionary<int, byte>() { { 4, 0x00 }, { 3, 0x02 }, { 2, 0x01 }, { 1, 0x02 } };
            if (data2[0] != map[int.Parse(LsmId)])
            {
                if (counter > 3)
                {
                    Debug.WriteLine("Fehlgeschlagen!");
                }
                else
                    await AllocSegment(ctrl, segType, counter + 1);
            }
        }


        private async Task WriteTableGroup(int addr)
        {
            //Alle verbundenen GAs finden und sortieren
            addedGroups = new List<string> { "" };
            foreach (DeviceComObject com in Device.ComObjects)
                foreach (GroupAddress group in com.Groups)
                    if (!addedGroups.Contains(group.GroupName))
                        addedGroups.Add(group.GroupName);
            addedGroups.Sort();

            //länge der Tabelle erstmal auf 1 setzen
            await dev.MemoryWriteSync(addr, new byte[] { 0x01 });

            await Task.Delay(100);
            //Gruppenadressen in Tabelle eintragen
            List<byte> data = new List<byte>();
            foreach (string group in addedGroups) //Liste zum Datenpaket hinzufügen
                if (group != "") data.AddRange(MulticastAddress.FromString(group).GetBytes());
            await dev.MemoryWriteSync(addr + 3, data.ToArray());

            //await Task.Delay(100);

            await dev.MemoryWriteSync(addr, new byte[] { BitConverter.GetBytes(addedGroups.Count)[0] });

            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                Device.LoadedGroup = true;
                Device.LoadedPA = true;
            });
        }


        private async Task WriteAssociationTable(int addr)
        {
            await dev.MemoryWriteSync(addr, new byte[] { 0x00 });


            //Erstelle Assoziationstabelle
            List<byte> data = new List<byte>();
            int sizeCounter = 0;

            foreach (DeviceComObject com in Device.ComObjects)
            {
                foreach (GroupAddress group in com.Groups)
                {
                    int indexG = addedGroups.IndexOf(group.GroupName);
                    int indexC = com.Number;

                    byte bIndexG = BitConverter.GetBytes(indexG)[0];
                    byte bIndexC = BitConverter.GetBytes(indexC)[0];
                    data.Add(bIndexG);
                    data.Add(bIndexC);
                    sizeCounter++;
                }
            }

            await dev.MemoryWriteSync(addr + 1, data.ToArray());

            await dev.MemoryWriteSync(addr, new byte[] { Convert.ToByte(sizeCounter) });

            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                Device.LoadedGroup = true;
            });
        }


        private async Task LsmState(XElement ctrl, int counter = 0)
        {
            byte[] data = new byte[11];
            int lsmIdx = int.Parse(ctrl.Attribute("LsmIdx").Value);
            int state = 1;
            switch (ctrl.Name.LocalName)
            {
                case "LdCtrlUnload":
                    state = (int)LoadStateMachineState.Unloaded;
                    break;
                case "LdCtrlLoad":
                    state = (int)LoadStateMachineState.Loading;
                    break;
                case "LdCtrlLoadCompleted":
                    state = (int)LoadStateMachineState.Loaded;
                    break;
            }
            int endU = (lsmIdx << 4) | state;
            data[0] = Convert.ToByte(endU);
            await dev.MemoryWriteSync(260, data);
            await Task.Delay(50);
            data = await dev.MemoryRead(46825 + lsmIdx, 1);

            Dictionary<int, byte> map = new Dictionary<int, byte>() { { 4, 0x00 }, { 3, 0x02 }, { 2, 0x01 }, { 1, 0x02 } };
            if(data[0] != map[(int)state]){
                if(counter > 3)
                {
                    Debug.WriteLine("Fehlgeschlagen!");
                } else
                    await LsmState(ctrl, counter + 1);
            }
        }




        private async void Start2()
        {
            BusDevice dev = new BusDevice(Device.LineName, Connection);
            dev.Connect();
            byte[] data = await dev.PropertyRead(3, 13, 5, 1);

            ProgressValue = 10;
            appId = BitConverter.ToString(data).Replace("-", "").Substring(8);
            appId = "M-" + appId.Substring(0, 4) + "_A-" + appId.Substring(4, 4) + "-" + appId.Substring(8, 2) + "-";

            //TODO Versionen beachten
            if (!Device.ApplicationId.StartsWith(appId))
            {
                TodoText = "Inkompatible Applikation";
                Finish();
                return;
            }


            addedGroups = new List<string> { "" };
            foreach (DeviceComObject com in Device.ComObjects)
                foreach (GroupAddress group in com.Groups)
                    if (!addedGroups.Contains(group.GroupName))
                        addedGroups.Add(group.GroupName);

            addedGroups.Sort();


            if (_type != ProgAppType.Komplett && Device.LoadedGroup)
            {
                State3();
                return;
            }
        }


        private async void Start()
        {
            TodoText = "Überprüfe Kompatibilität...";
            // Connect
            TunnelRequest builder = new TunnelRequest();
            //builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Parser.ApciTypes.Connect, _sequence, 255);
            Connection.Send(builder);
            await Task.Delay(100);
            _sequence++;

            builder = new TunnelRequest();
            byte[] data = { 3, 13, 0x01 << 4, 0x01 }; // TODO probiere 5 ob ganze appID und Start bei 0!
            //builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Parser.ApciTypes.PropertyValueRead, _sequence, _currentSeqNum, data);
            Connection.Send(builder);
        }


        private async void State1(TunnelResponse response)
        {
            ProgressValue = 10;
            appId = BitConverter.ToString(response.Data).Replace("-", "").Substring(8);
            appId = "M-" + appId.Substring(0, 4) + "_A-" + appId.Substring(4, 4) + "-" + appId.Substring(8, 2) + "-";

            //TODO Versionen beachten
            if (!Device.ApplicationId.StartsWith(appId))
            {
                TodoText = "Inkompatible Applikation";
                Finish();
                return;
            }


            addedGroups = new List<string> { "" };
            foreach (DeviceComObject com in Device.ComObjects)
                foreach (GroupAddress group in com.Groups)
                    if (!addedGroups.Contains(group.GroupName))
                        addedGroups.Add(group.GroupName);

            addedGroups.Sort();


            if (_type != ProgAppType.Komplett && Device.LoadedGroup)
            {
                State3();
                return;
            }



            CatalogContext context = new CatalogContext();
            ApplicationViewModel appModel = context.Applications.Single(a => a.Id.StartsWith(appId));
            int GroupAddress = 0;

            if (appModel.Table_Group != "" || appModel.Table_Group != null)
            {
                AppAbsoluteSegmentViewModel segmentModel = context.AppAbsoluteSegments.Single(s => s.Id == appModel.Table_Group);
                GroupAddress = segmentModel.Address;
            }
            else
            {
                //TODO hinzufügen von Adresse aus der Maske holen!
                XDocument master = await GetKnxMaster();
                XElement mask = master.Descendants(XName.Get("MaskVersion", master.Root.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == appModel.Mask);
                XElement resource = mask.Descendants(XName.Get("Resource", master.Root.Name.NamespaceName)).Single(l => l.Attribute("Name").Value == "GroupAssociationTable"); // "GroupAddressTable");
                XElement location = resource.Element(XName.Get("Location", master.Root.Name.NamespaceName));

                switch (location.Attribute("AddressSpace").Value)
                {
                    case "StandardMemory":
                        bool flag = int.TryParse(location.Attribute("StartAddress").Value, out GroupAddress);
                        if (!flag)
                        {
                            State2();
                            return;
                        }
                        break;

                    case "Pointer":
                        string pointer = location.Attribute("PtrResource").Value;
                        resource = mask.Descendants(XName.Get("Resource", master.Root.Name.NamespaceName)).Single(l => l.Attribute("Name").Value == pointer);
                        location = resource.Element(XName.Get("Location", master.Root.Name.NamespaceName));
                        //TODO: Property auslesen
                        break;


                    default:
                        throw new NotImplementedException(location.Attribute("AddressSpace").Value + "; " + resource.ToString());

                }

            }

            TodoText = "Schreibe Gruppentabelle....";
            ProgressValue = 20;

            //Länge der Tabelle auf 1 stellen
            _sequence++;
            TunnelRequest builder = new TunnelRequest();
            List<byte> data = new List<byte> { 1 };
            byte[] address = BitConverter.GetBytes(Convert.ToInt16(GroupAddress));
            Array.Reverse(address);
            data.AddRange(address);
            data.Add(1);
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Kaenx.Konnect.Parser.ApciTypes.MemoryWrite, _sequence, _currentSeqNum++, data.ToArray());
            Connection.Send(builder);

            await Task.Delay(100);


            //Tabelle mit den Gruppenadressen füllen
            _sequence++;
            builder = new TunnelRequest();
            data = new List<byte> { 0 }; //Länge wird später richtig gesetzt
            address = BitConverter.GetBytes(Convert.ToInt16(GroupAddress + 3));
            Array.Reverse(address);
            data.AddRange(address);

            //Liste zum Datenpaket hinzufügen
            foreach (string group in addedGroups)
                if(group != "")
                    data.AddRange(MulticastAddress.FromString(group).GetBytes());

            //Datenlänge richtig setzen
            data[0] = BitConverter.GetBytes(addedGroups.Count * 2)[0];

            //builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Parser.ApciTypes.MemoryWrite, _sequence, _currentSeqNum++, data.ToArray());
            Connection.Send(builder);

            await Task.Delay(1000);

            _sequence++;
            builder = new TunnelRequest();
            data = new List<byte> { 1 };
            address = BitConverter.GetBytes(Convert.ToInt16(GroupAddress));
            Array.Reverse(address);
            data.AddRange(address);
            data.Add(BitConverter.GetBytes(addedGroups.Count)[0]);
            //builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Parser.ApciTypes.MemoryWrite, _sequence, _currentSeqNum++, data.ToArray());
            Connection.Send(builder);


            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                Device.LoadedGroup = true;
            });


            State2();
        }


        private async void State2()
        {
            TodoText = "Schreibe Assoziationstabelle....";
            ProgressValue = 30;


            //check if table already written

            
            ApplicationViewModel appModel = _context.Applications.Single(a => a.Id.StartsWith(appId));
            int AssoAddress = 0;

            if (appModel.Table_Group != "" || appModel.Table_Group != null)
            {
                AppAbsoluteSegmentViewModel segmentModel = _context.AppAbsoluteSegments.Single(s => s.Id == appModel.Table_Assosiations);
                AssoAddress = segmentModel.Address;
            }
            else
            {
                
            }


            //_sequence++;
            //TunnelRequest builder = new TunnelRequest();
            //List<byte> data = new List<byte> { 12 };
            //byte[] address = BitConverter.GetBytes(Convert.ToInt16(AssoAddress));
            //Array.Reverse(address);
            //data.AddRange(address);
            //builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.MemoryRead, _sequence, _currentSeqNum++, data.ToArray());
            //Connection.Send(builder);
            //_state = 3;



            //Länge der Tabelle auf 0 setzen.
            _sequence++;
            TunnelRequest builder = new TunnelRequest();
            List<byte> data = new List<byte> { 1 };
            byte[] address = BitConverter.GetBytes(Convert.ToInt16(AssoAddress));
            Array.Reverse(address);
            data.AddRange(address);
            data.Add(0);
            //builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.MemoryWrite, _sequence, _currentSeqNum++, data.ToArray());
            Connection.Send(builder);

            await Task.Delay(100);


            //Erstelle Assoziationstabelle
            data = new List<byte>();
            int sizeCounter = 0;


            foreach (DeviceComObject com in Device.ComObjects)
            {
                foreach (GroupAddress group in com.Groups)
                {
                    int indexG = addedGroups.IndexOf(group.GroupName) + 1;
                    int indexC = com.Number;

                    byte bIndexG = BitConverter.GetBytes(indexG)[0];
                    byte bIndexC = BitConverter.GetBytes(indexC)[0];
                    data.Add(bIndexG);
                    data.Add(bIndexC);
                    sizeCounter++;
                }
            }

            //Schreibe Tabelle
            _sequence++;
            builder = new TunnelRequest();
            address = BitConverter.GetBytes(Convert.ToInt16(AssoAddress + 1));
            Array.Reverse(address);
            data.AddRange(address);
            //builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.MemoryWrite, _sequence, _currentSeqNum++, data.ToArray());
            Connection.Send(builder);

            //Länge der Tabelle korrekt setzen
            _sequence++;
            builder = new TunnelRequest();
            data = new List<byte> { 1 };
            address = BitConverter.GetBytes(Convert.ToInt16(AssoAddress));
            Array.Reverse(address);
            data.AddRange(address);
            data.Add(BitConverter.GetBytes(sizeCounter)[0]);
            //builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.MemoryWrite, _sequence, _currentSeqNum++, data.ToArray());
            Connection.Send(builder);


            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
              {
                  Device.LoadedGroup = true;
              });

            State3();
        }


        private async void State3()
        {

            if (_type != ProgAppType.Komplett && Device.LoadedApplication)
            {
                Finish();
                return;
            }


            if(_type == ProgAppType.Komplett)
            {
                AppAbsoluteSegmentViewModel segment = _context.AppAbsoluteSegments.Single(a => a.Id == "M-0083_A-000D-11-B2F5_AS-4400");
                byte[] data = Convert.FromBase64String(segment.Data);


            }


            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                Device.LoadedApplication = true;
            });


            Finish();
        }




        private void Finish()
        {

            Finished(this, null);
        }



        private void Changed(string name)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            catch
            {
                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                });
            }
        }

        private async Task<XDocument> GetKnxMaster()
        {
            StorageFile masterFile;

            try
            {
                masterFile = await ApplicationData.Current.LocalFolder.GetFileAsync("knx_master.xml");
            }
            catch
            {
                StorageFile defaultFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/knx_master.xml"));
                masterFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("knx_master.xml");
                await FileIO.WriteTextAsync(masterFile, await FileIO.ReadTextAsync(defaultFile));
            }


            XDocument masterXml = XDocument.Load(await masterFile.OpenStreamForReadAsync());
            return masterXml;
        }


       private enum LoadStateMachineState
        {
            Undefined,
            Loading,
            Loaded,
            Error,
            Unloaded
        }

        public enum ProgAppType
        {
            Komplett, // 0
            Partiell, // 1
            Minimal // 2
        }
    }
}
