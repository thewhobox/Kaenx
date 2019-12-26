using METS.Classes.Project;
using METS.Context.Catalog;
using METS.Knx;
using METS.Knx.Addresses;
using METS.Knx.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;

namespace METS.Classes.Bus.Actions
{
    public class ProgApplication : IBusAction, INotifyPropertyChanged
    {
        private ProgAppType _type;
        private int _state = 0;
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private byte _sequence = 0x00;
        private CancellationToken _token;
        private byte _currentSeqNum = 0;
        private string appId;

        public string Type { get; } = "Geräte Info";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public Connection Connection { get; set; }

        public event EventHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;

        public ProgApplication(ProgAppType type)
        {
            _type = type;
        }

        private void _conn_OnTunnelResponse(TunnelResponse response)
        {
            bool sendAck = true;

            if (_state == 1 && response.APCI == Knx.Parser.ApciTypes.PropertyValueResponse)
            {
                State1(response);
            }
            else if(_state == 3 && response.APCI == Knx.Parser.ApciTypes.MemoryResponse)
            {

            }
            else
            {
                sendAck = false;
            }

            if (!sendAck) return;
            _sequence++;
            TunnelRequest builder = new TunnelRequest(); //TODO ack wieder gängig machen
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.Ack, _sequence, BitConverter.GetBytes(response.SequenceNumber)[0]);
            Connection.Send(builder);
        }

        public void Run(CancellationToken token)
        {
            Connection.OnTunnelRequest += _conn_OnTunnelResponse;
            _token = token;
            _state = 0;

            Start();
        }

        private async void Start()
        {
            TodoText = "Überprüfe Kompatibilität...";
            // Connect
            TunnelRequest builder = new TunnelRequest();
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.Connect, _sequence, 255);
            Connection.Send(builder);
            await Task.Delay(100);
            _sequence++;

            builder = new TunnelRequest();
            byte[] data = { 3, 13, 0x01 << 4, 0x01 };
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.PropertyValueRead, _sequence, _currentSeqNum, data);
            Connection.Send(builder);
            _state = 1;
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


            if(_type == ProgAppType.Minimal && Device.LoadedGroups)
            {
                State2();
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
            }

            XDocument master = await GetKnxMaster();
            XElement mask = master.Descendants(XName.Get("MaskVersion", master.Root.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == appModel.Mask);
            XElement resource = mask.Descendants(XName.Get("Resource", master.Root.Name.NamespaceName)).Single(l => l.Attribute("Name").Value == "GroupAssociationTable"); // "GroupAddressTable");
            XElement location = resource.Element(XName.Get("Location", master.Root.Name.NamespaceName));

            switch (location.Attribute("AddressSpace").Value)
            {
                case "StandardMemory":
                    bool flag = int.TryParse(location.Attribute("StartAddress").Value, out GroupAddress);
                    if(!flag)
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



            TodoText = "Schreibe Gruppentabelle....";
            ProgressValue = 20;

            //Lenge der Tabelle auf 1 stellen
            _sequence++;
            TunnelRequest builder = new TunnelRequest();
            List<byte> data = new List<byte> { 1 };
            byte[] address = BitConverter.GetBytes(Convert.ToInt16(GroupAddress));
            Array.Reverse(address);
            data.AddRange(address);
            data.Add(1);
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.MemoryWrite, _sequence, _currentSeqNum++, data.ToArray());
            Connection.Send(builder);

            await Task.Delay(1000);


            //Tabelle mit den Gruppenadressen füllen
            _sequence++;
            builder = new TunnelRequest();
            data = new List<byte> { 0 }; //Länge wird später richtig gesetzt
            address = BitConverter.GetBytes(Convert.ToInt16(GroupAddress + 3));
            Array.Reverse(address);
            data.AddRange(address);

            //Liste zusammenstelle und sortieren
            List<string> addedGroups = new List<string>();
            foreach (DeviceComObject com in Device.ComObjects)
                foreach (GroupAddress group in com.Groups)
                    if (!addedGroups.Contains(group.GroupName))
                        addedGroups.Add(group.GroupName);

            addedGroups.Sort();

            //Liste zum Datenpaket hinzufügen
            foreach (string group in addedGroups)
                data.AddRange(MulticastAddress.FromString(group).GetBytes());

            //Datenlänge richtig setzen
            data[0] = BitConverter.GetBytes(addedGroups.Count * 2)[0];

            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.MemoryWrite, _sequence, _currentSeqNum++, data.ToArray());
            Connection.Send(builder);

            await Task.Delay(1000);

            _sequence++;
            builder = new TunnelRequest();
            data = new List<byte> { 1 };
            address = BitConverter.GetBytes(Convert.ToInt16(GroupAddress));
            Array.Reverse(address);
            data.AddRange(address);
            data.Add(6);
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.MemoryWrite, _sequence, _currentSeqNum++, data.ToArray());
            Connection.Send(builder);


            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                Device.LoadedGroups = true;
            });


            State2();
        }


        private async void State2()
        {
            TodoText = "Schreibe Assoziationstabelle....";
            ProgressValue = 30;


            //check if table already written

            CatalogContext context = new CatalogContext();
            ApplicationViewModel appModel = context.Applications.Single(a => a.Id.StartsWith(appId));
            int ComAddress = 0;

            if (appModel.Table_Group != "" || appModel.Table_Group != null)
            {
                AppAbsoluteSegmentViewModel segmentModel = context.AppAbsoluteSegments.Single(s => s.Id == appModel.Table_Object);
                ComAddress = segmentModel.Address;
            }
            else
            {
                
            }

            


            _sequence++;
            TunnelRequest builder = new TunnelRequest();
            List<byte> data = new List<byte> { 12 };
            byte[] address = BitConverter.GetBytes(Convert.ToInt16(ComAddress + 2));
            Array.Reverse(address);
            data.AddRange(address);
            builder.Build(UnicastAddress.FromString("0.0.0"), UnicastAddress.FromString(Device.LineName), Knx.Parser.ApciTypes.MemoryRead, _sequence, _currentSeqNum++, data.ToArray());
            Connection.Send(builder);
            _state = 3;




            //Finish();
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


        public enum ProgAppType
        {
            Komplett, // 0
            Partiell, // 1
            Minimal // 2
        }
    }
}
