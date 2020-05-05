using Kaenx.Classes.Buildings;
using Kaenx.Classes.Dynamic;
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


            Start();
        }


        private async void Start()
        {
            dev = new BusDevice(Device.LineName, Connection);
            TodoText = "Applikation schreiben";


            CatalogContext _context = new CatalogContext();
            AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == Device.ApplicationId);
            ApplicationViewModel app = _context.Applications.Single(a => a.Id == Device.ApplicationId);

            XElement temp;
            XElement procedure = null;

            switch (app.LoadProcedure)
            {
                case LoadProcedureTypes.Unknown:
                    TodoText = "LadeProzedur ist unbekannt";
                    Finished?.Invoke(this, null);
                    break;

                case LoadProcedureTypes.Product:
                    temp = XDocument.Parse(Encoding.UTF8.GetString(adds.LoadProcedures)).Root;
                    procedure = temp.Descendants(XName.Get("LoadProcedure", temp.Name.NamespaceName)).First();
                    break;

                case LoadProcedureTypes.Default:
                    temp = await GetKnxMaster();
                    temp = temp.Descendants(XName.Get("MaskVersion", temp.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == app.Mask);
                    procedure = temp.Descendants(XName.Get("Procedure", temp.Name.NamespaceName)).First(m => m.Attribute("ProcedureType").Value == "Load"); //TODO beachte ob komplett, minimal, etc
                    break;

                case LoadProcedureTypes.Merge:
                    XElement temp2 = XDocument.Parse(Encoding.UTF8.GetString(adds.LoadProcedures)).Root;
                    temp = await GetKnxMaster();
                    temp = temp.Descendants(XName.Get("MaskVersion", temp.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == app.Mask);
                    temp = temp.Descendants(XName.Get("Procedure", temp.Name.NamespaceName)).First(m => m.Attribute("ProcedureType").Value == "Load"); //TODO beachte ob komplett, minimal, etc

                    IEnumerable<XElement> merges = temp2.Descendants(XName.Get("LoadProcedure", temp.Name.NamespaceName));

                    while (temp.Descendants(XName.Get("LdCtrlMerge", temp.Name.NamespaceName)).Count() > 0)
                    {
                        XElement merge = temp.Descendants(XName.Get("LdCtrlMerge", temp.Name.NamespaceName)).First();
                        if (!merges.Any(m => m.Attribute("MergeId").Value == merge.Attribute("MergeId").Value))
                        {
                            merge.Remove();
                            continue;
                        }

                        XElement corres = merges.Single(m => m.Attribute("MergeId").Value == merge.Attribute("MergeId").Value);
                        merge.PreviousNode.AddAfterSelf(corres.Elements());
                        merge.Remove();

                    }
                    procedure = temp;
                    break;
            }



            double stepSize = 100.0 / procedure.Elements().Count();
            double currentProg = 0;
            Debug.WriteLine("StepSize: " + stepSize + " - " + procedure.Elements().Count());

            foreach(XElement ctrl in procedure.Elements())
            {
                if (_token.IsCancellationRequested) 
                    return;

                currentProg += stepSize;
                ProgressValue = (int)currentProg;

                Debug.WriteLine(ctrl.Name.LocalName);
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
                        byte[] prop = await dev.PropertyRead(Convert.ToByte(obj), Convert.ToByte(pid));
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
                        await LsmState(ctrl);
                        break;

                    case "LdCtrlTaskSegment":
                        await AllocSegment(ctrl, 2);
                        break;

                    case "LdCtrlAbsSegment":
                        await WriteAbsSegment(ctrl, adds);
                        break;

                    case "LdCtrlRelSegment":

                        break;

                    case "LdCtrlWriteProp":
                        //nicht ausgereift
                        byte[] data = new byte[2];
                        uint num = uint.Parse(ctrl.Attribute("InlineData").Value, System.Globalization.NumberStyles.AllowHexSpecifier);
                        byte[] floatVals = BitConverter.GetBytes(num);
                        await dev.PropertyWrite(Convert.ToByte(ctrl.Attribute("ObjIdx").Value), Convert.ToByte(ctrl.Attribute("PropId").Value), data);
                        break;

                    case "LdCtrlWriteRelMem":

                        break;

                    case "LdCtrlDelay":
                        int ms = int.Parse(ctrl.Attribute("MilliSeconds").Value);
                        await Task.Delay(ms);
                        break;

                    default:
                        Debug.WriteLine("Unbekanntes Element: " + ctrl.Name.LocalName);
                        break;
                }
            }

            TodoText = "Abgeschlossen";
            ProgressValue = 100;

            Finished?.Invoke(this, null);
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
                    try
                    {
                        await WriteApplication(adds);
                    }
                    catch
                    {

                    }
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
            Dictionary<string, AppSegmentViewModel> mems = new Dictionary<string, AppSegmentViewModel>();
            Dictionary<string, AppParameterTypeViewModel> types = new Dictionary<string, AppParameterTypeViewModel>();
            List<int> changed = new List<int>();

            foreach (AppParameterTypeViewModel type in _context.AppParameterTypes)
                types.Add(type.Id, type);

            foreach(AppParameter para in paras)
            {
                if (para.SegmentId == null) continue;
                if (!mems.ContainsKey(para.SegmentId))
                {
                    AppSegmentViewModel seg = _context.AppSegments.Single(a => a.Id == para.SegmentId);
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


            TodoText = "Schreibe Speicher...";

            switch (_type)
            {
                case ProgAppType.Komplett:
                    foreach (AppSegmentViewModel seg in mems.Values)
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
            Dictionary<string, ChangeParamModel> ParaChanges = new Dictionary<string, ChangeParamModel>();
            List<AppParameter> paras = new List<AppParameter>();

            foreach (AppParameter para in _context.AppParameters.Where(p => p.ApplicationId == Device.ApplicationId))
                AppParas.Add(para.Id, para);

            ProjectContext _c = SaveHelper.contextProject;

            if (_c.ChangesParam.Any(c => c.DeviceId == Device.UId))
            {
                var changes = _c.ChangesParam.Where(c => c.DeviceId == Device.UId).OrderByDescending(c => c.StateId);
                foreach (ChangeParamModel model in changes)
                {
                    if (ParaChanges.ContainsKey(model.ParamId)) continue;
                    ParaChanges.Add(model.ParamId, model);
                }
            }

            Dictionary<string, ViewParamModel> Id2Param = new Dictionary<string, ViewParamModel>();
            List<IDynChannel> Channels = SaveHelper.ByteArrayToObject<List<IDynChannel>>(adds.ParamsHelper, true);

            foreach (IDynChannel ch in Channels)
            {
                foreach (ParameterBlock block in ch.Blocks)
                {
                    foreach (IDynParameter para in block.Parameters)
                    {
                        if (ParaChanges.ContainsKey(para.Id))
                            para.Value = ParaChanges[para.Id].Value;

                        if (!Id2Param.ContainsKey(para.Id))
                            Id2Param.Add(para.Id, new ViewParamModel(para.Value));

                        Id2Param[para.Id].Parameters.Add(para);
                    }
                }
            }


            foreach (IDynChannel ch in Channels)
            {
                bool visible = SaveHelper.CheckConditions(ch.Conditions, Id2Param);

                if (visible)
                {
                    foreach (ParameterBlock block in ch.Blocks)
                    {
                        bool vis2 = SaveHelper.CheckConditions(block.Conditions, Id2Param);

                        if (vis2)
                        {
                            foreach (IDynParameter para in block.Parameters)
                            {
                                bool vis3 = SaveHelper.CheckConditions(para.Conditions, Id2Param);
                                if (vis3)
                                {
                                    AppParameter xpara = AppParas[para.Id];
                                    if (!paras.Contains(xpara))
                                    {
                                        xpara.Value = para.Value;
                                        paras.Add(xpara);
                                    }
                                }
                            }
                        }
                        
                    }
                }

                
            }


            return paras;
        }



        private bool CheckConditions(Dictionary<string, AppParameter> paras, List<Dynamic.ParamCondition> conds)
        {
            bool isVisible = true;

            foreach (Dynamic.ParamCondition cond in conds)
            {
                if (!isVisible) break;

                AppParameter para = paras[cond.SourceId];
                switch (cond.Operation)
                {
                    case Dynamic.ConditionOperation.IsInValue:
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
                foreach (FunctionGroup group in com.Groups)
                    if (!addedGroups.Contains(group.Address.ToString()))
                        addedGroups.Add(group.Address.ToString());
            addedGroups.Sort();

            //länge der Tabelle erstmal auf 1 setzen
            Debug.WriteLine("Tabelle auf 1");
            await dev.MemoryWriteSync(addr, new byte[] { 0x01 });

            await Task.Delay(100);
            //Gruppenadressen in Tabelle eintragen
            List<byte> data = new List<byte>();
            foreach (string group in addedGroups) //Liste zum Datenpaket hinzufügen
                if (group != "") data.AddRange(MulticastAddress.FromString(group).GetBytes());
            Debug.WriteLine("Tabelle schreiben");
            await dev.MemoryWriteSync(addr + 3, data.ToArray());

            await Task.Delay(100);

            Debug.WriteLine("Tabelle länge setzen");
            await dev.MemoryWriteSync(addr, new byte[] { BitConverter.GetBytes(addedGroups.Count)[0] });


            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                Device.LoadedGroup = true;
                Device.LoadedApplication = true;
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
                foreach (FunctionGroup group in com.Groups)
                {
                    int indexG = addedGroups.IndexOf(group.Address.ToString());
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

        private async Task<XElement> GetKnxMaster()
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
            return masterXml.Root;
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
