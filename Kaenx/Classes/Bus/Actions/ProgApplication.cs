using Kaenx.Classes.Helper;
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
                        await WriteAbsSegment(ctrl, adds);
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


        private async Task WriteAbsSegment(XElement ctrl, AppAdditional adds)
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
                    await WriteApplication(adds);
                    break;

                case "4":
                    //TODO später
                    break;
            }
        }

        private async Task WriteApplication(AppAdditional adds)
        {
            TodoText = "Berechne Speicher...";
            List<AppParameter> paras = GetVisibleParams(adds);
            Dictionary<string, byte[]> memsData = new Dictionary<string, byte[]>();
            Dictionary<string, AppAbsoluteSegmentViewModel> mems = new Dictionary<string, AppAbsoluteSegmentViewModel>();
            Dictionary<string, AppParameterTypeViewModel> types = new Dictionary<string, AppParameterTypeViewModel>();
            List<int> changed = new List<int>();

            foreach (AppParameterTypeViewModel type in _context.AppParameterTypes)
                types.Add(type.Id, type);

            foreach(AppParameter para in paras)
            {
                if (para.SegmentId == null) continue;
                if (!mems.ContainsKey(para.SegmentId))
                {
                    AppAbsoluteSegmentViewModel seg = _context.AppAbsoluteSegments.Single(a => a.Id == para.SegmentId);
                    memsData[para.SegmentId] = Convert.FromBase64String(seg.Data);
                    mems[para.SegmentId] = seg;
                }

                AppParameterTypeViewModel type = types[para.ParameterTypeId];

                byte[] paraData = type.Size >= 8 ? new byte[type.Size / 8] : new byte[1];

                switch (type.Type)
                {
                    case ParamTypes.Enum:
                    case ParamTypes.NumberUInt:
                        paraData = BitConverter.GetBytes(Convert.ToUInt32(para.Value)).Take(type.Size / 8).ToArray();
                        Array.Reverse(paraData);
                        break;
                    case ParamTypes.NumberInt:
                        paraData = BitConverter.GetBytes(Convert.ToInt32(para.Value)).Take(type.Size / 8).ToArray();
                        Array.Reverse(paraData);
                        break;

                    case ParamTypes.Text:
                        paraData = Encoding.UTF8.GetBytes(para.Value);
                        break;
                }



                if(type.Size >= 8)
                {
                    byte[] memory = memsData[para.SegmentId];
                    for (int i = 0; i < type.Size / 8; i++)
                    {
                        memory[para.Offset + i] = paraData[i];
                        changed.Add(para.Offset + i);
                    }
                }
            }

            switch (_type)
            {
                case ProgAppType.Komplett:
                    foreach (AppAbsoluteSegmentViewModel seg in mems.Values)
                    {
                        await dev.MemoryWriteSync(seg.Address, memsData[seg.Id]);
                    }
                    break;

                case ProgAppType.Minimal:
                    break;

                case ProgAppType.Partiell:
                    break;
            }

            


            

        }




        private List<AppParameter> GetVisibleParams(AppAdditional adds)
        {
            Dictionary<string, AppParameter> AppParas = new Dictionary<string, AppParameter>();
            List<AppParameter> paras = new List<AppParameter>();
            XDocument dynamic = XDocument.Parse(Encoding.UTF8.GetString(adds.Dynamic));

            foreach (AppParameter para in _context.AppParameters.Where(p => p.ApplicationId == Device.ApplicationId))
                AppParas.Add(para.Id, para);

            ProjectContext _contextP = SaveHelper.contextProject;

            if (_contextP.ChangesParam.Any(c => c.DeviceId == Device.UId))
            {
                List<string> updated = new List<string>();
                var changes = _contextP.ChangesParam.Where(c => c.DeviceId == Device.UId).OrderByDescending(c => c.StateId);
                foreach (ChangeParamModel model in changes)
                {
                    if (updated.Contains(model.ParamId)) continue;
                    AppParas[model.ParamId].Value = model.Value;
                    updated.Add(model.ParamId);
                }
            }

            Dictionary<string, ParamVisHelper> conditions = new Dictionary<string, ParamVisHelper>();
            foreach (ParamVisHelper helper in SaveHelper.ByteArrayToObject<List<ParamVisHelper>>(adds.ParameterAll))
                conditions.Add(helper.ParameterId, helper);

            int visibleBlocks = 0;
            foreach (XElement paraBlock in dynamic.Descendants(XName.Get("ParameterBlock", dynamic.Root.Name.NamespaceName)))
            {
                XElement parent = paraBlock;
                XElement lastWhen = null;
                bool stop = false;
                bool isVisibleBlock = true;
                int lastNr;

                while (!stop)
                {
                    parent = parent.Parent;

                    switch (parent.Name.LocalName)
                    {
                        case "Dynamic":
                            stop = true;
                            break;

                        case "when":
                            lastWhen = parent;
                            break;

                        case "choose":
                            //TODO choose ausweiten auf < > <= >=
                            AppParameter cPara = AppParas[parent.Attribute("ParamRefId").Value];
                            if (lastWhen.Attribute("default") != null) //Test default
                            {

                            }
                            else if (int.TryParse(lastWhen.Attribute("test")?.Value, out lastNr)) //Test isequal
                            {
                                if (cPara.Value != lastWhen.Attribute("test").Value)
                                {
                                    isVisibleBlock = false;
                                    stop = true;
                                }
                            }
                            else if (lastWhen.Attribute("test").Value.Contains(" "))
                            {
                                if (!lastWhen.Attribute("test").Value.Split(" ").Contains(cPara.Value)) //Test contains
                                {
                                    isVisibleBlock = false;
                                    stop = true;
                                }
                            }
                            break;
                    }
                }

                if (!isVisibleBlock) continue;
                visibleBlocks++;

                foreach(XElement param in paraBlock.Descendants(XName.Get("ParameterRefRef", paraBlock.Name.NamespaceName)))
                {
                    string paraId = param.Attribute("RefId").Value;
                    ParamVisHelper helper = conditions[paraId];

                    if (CheckConditions(AppParas, helper.Conditions))
                        paras.Add(AppParas[paraId]);
                }
            }





            return paras;
        }



        private bool CheckConditions(Dictionary<string, AppParameter> paras, List<Controls.Paras.ParamCondition> conds)
        {
            bool isVisible = true;

            foreach (Controls.Paras.ParamCondition cond in conds)
            {
                if (!isVisible) break;

                AppParameter para = paras[cond.SourceId];
                switch (cond.Operation)
                {
                    case Controls.Paras.ConditionOperation.IsInValue:
                        if (!cond.Values.Split(",").Contains(para.Value))
                            isVisible = false;
                        break;
                }
            }

            return isVisible;
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

            Dictionary<int, byte> map = new Dictionary<int, byte>() { { 4, 0x00 }, { 3, 0x02 }, { 2, 0x02 }, { 1, 0x02 } };
            if (data2[0] != map[int.Parse(LsmId)])
            {
                if (counter > 2)
                {
                    Debug.WriteLine("Fehlgeschlagen!");
                }
                else
                    await AllocSegment(ctrl, segType, counter + 1);
            }
        }


        private async Task WriteTableGroup(int addr)
        {
            TodoText = "Schreibe Gruppentabelle...";

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
            TodoText = "Schreibe Assozationstabelle...";

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
