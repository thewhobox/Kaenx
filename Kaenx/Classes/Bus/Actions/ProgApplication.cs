using Kaenx.Classes.Buildings;
using Kaenx.Classes.Dynamic;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Connections;
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
        private bool _alreadyFinished = false;
        private string _todoText;
        private CancellationToken _token;
        private List<string> addedGroups;
        private CatalogContext _context = new CatalogContext();


        private List<byte> dataGroupTable = new List<byte>();
        private List<byte> dataAssoTable = new List<byte>();
        private Dictionary<string, AppSegmentViewModel> dataSegs = new Dictionary<string, AppSegmentViewModel>();
        private Dictionary<string, byte[]> dataMems = new Dictionary<string, byte[]>();
        private Dictionary<string, int> dataAddresses = new Dictionary<string, int>();

        public string Type { get; set; }
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        private ProcedureTypes _procedureType = ProcedureTypes.Load;
        public ProcedureTypes ProcedureType
        {
            get { return _procedureType; }
            set
            {
                _procedureType = value;
                Type = _procedureType == ProcedureTypes.Load ? "Applikation" : "Entladen";
            }
        }


        public UnloadHelper Helper;
        public IKnxConnection Connection { get; set; }
        private ApplicationViewModel app;

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
            TodoText = ProcedureType == ProcedureTypes.Load ? "Applikation schreiben" : "Gerät entladen";


            if (ProcedureType == ProcedureTypes.Load || (Helper != null && (Helper.UnloadApplication || Helper.UnloadBoth)))
            {
                CatalogContext _context = new CatalogContext();
                AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == Device.ApplicationId);
                app = _context.Applications.Single(a => a.Id == Device.ApplicationId);

                XElement temp;
                XElement procedure = null;
                string procedureType = "Load";
                if (ProcedureType == ProcedureTypes.Unload)
                {
                    app.LoadProcedure = LoadProcedureTypes.Default;
                    procedureType = "Unload";
                }

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
                        procedure = temp.Descendants(XName.Get("Procedure", temp.Name.NamespaceName)).First(m => m.Attribute("ProcedureType").Value == procedureType); //TODO beachte ob komplett, minimal, etc
                        break;

                    case LoadProcedureTypes.Merge:
                        XElement temp2 = XDocument.Parse(Encoding.UTF8.GetString(adds.LoadProcedures)).Root;
                        temp = await GetKnxMaster();
                        temp = temp.Descendants(XName.Get("MaskVersion", temp.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == app.Mask);
                        temp = temp.Descendants(XName.Get("Procedure", temp.Name.NamespaceName)).First(m => m.Attribute("ProcedureType").Value == procedureType); //TODO beachte ob komplett, minimal, etc

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


                try
                {

                    foreach (XElement ctrl in procedure.Elements())
                    {
                        if (_token.IsCancellationRequested)
                            return;

                        currentProg += stepSize;
                        ProgressValue = (int)currentProg;

                        Debug.WriteLine(ctrl.Name.LocalName);
                        switch (ctrl.Name.LocalName)
                        {
                            case "LdCtrlConnect":
                                await dev.Connect();
                                break;

                            case "LdCtrlDisconnect":
                                dev.Disconnect();
                                break;

                            case "LdCtrlRestart":
                                await dev.Restart();
                                break;

                            case "LdCtrlCompareProp":
                                int obj = int.Parse(ctrl.Attribute("ObjIdx").Value);
                                int pid = int.Parse(ctrl.Attribute("PropId").Value);
                                byte[] prop = await dev.PropertyRead(Convert.ToByte(obj), Convert.ToByte(pid));
                                string dataCP = ctrl.Attribute("InlineData").Value;

                                if (!dataCP.StartsWith(BitConverter.ToString(prop).Replace("-", "")))
                                {
                                    TodoText = "Fehler beim schreiben! PAx01";
                                    dev.Disconnect();
                                    if (!_alreadyFinished)
                                    {
                                        _alreadyFinished = true;
                                        Finished?.Invoke(this, null);
                                    }
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
                                await AllocRelSegment(adds, ctrl);
                                // 03_05_02 - Seite 116
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

                            case "LdCtrlWriteMem":
                                await WriteMemory(adds, ctrl);
                                break;

                            default:
                                Debug.WriteLine("Unbekanntes Element: " + ctrl.Name.LocalName);
                                break;
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    TodoText = "Gerät antwortet nicht";
                    ProgressValue = 100;
                    if (!_alreadyFinished)
                    {
                        _alreadyFinished = true;
                        Finished?.Invoke(this, null);
                    }
                }
            }



            if(Helper != null && (Helper.UnloadAddress || Helper.UnloadBoth))
            {
                await dev.Connect();
                string mask = await dev.DeviceDescriptorRead();
                mask = "MV-" + mask;
                await dev.RessourceWrite("ProgrammingMode", new byte[] { 0x01 });
                dev.Disconnect();
                BusCommon comm = new BusCommon(Connection);
                comm.IndividualAddressWrite(UnicastAddress.FromString("15.15.255"));
                await Task.Delay(200);
                BusDevice dev2 = new BusDevice("15.15.255", Connection);
                await dev2.Connect();
                await dev2.Restart();
                dev2.Disconnect();
            }



            TodoText = "Abgeschlossen";
            ProgressValue = 100;

            if (!_alreadyFinished)
            {
                _alreadyFinished = true;
                Finished?.Invoke(this, null);
            }
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Hier lief was schief... " + ex.Message);
                    }
                    break;

                case "4":
                    //TODO später
                    break;
            }
        }


        private async Task WriteMemory(AppAdditional adds, XElement ctrl)
        {
            if (dataMems.Count == 0)
            {
                TodoText = "Berechne Speicher...";
                GenerateApplication(adds);
            }


            if (dataAssoTable.Count == 0 || dataGroupTable.Count == 0)
            {
                TodoText = "Berechne Tabellen...";
                GenerateGroupTable();
                GenerateAssoTable();
                await Task.Delay(100);



                dataMems[app.Table_Group][app.Table_Group_Offset] = Convert.ToByte(addedGroups.Count);
                for (int i = 0; i < dataGroupTable.Count; i++)
                {
                    dataMems[app.Table_Group][i + app.Table_Group_Offset + 3] = dataGroupTable[i];
                }

                dataMems[app.Table_Assosiations][app.Table_Assosiations_Offset] = Convert.ToByte(dataAssoTable.Count / 2);
                for (int i = 0; i < dataAssoTable.Count; i++)
                {
                    dataMems[app.Table_Assosiations][i + app.Table_Assosiations_Offset + 1] = dataAssoTable[i];
                }
            }

            TodoText = "Schreibe Speicher...";

            byte[] value;
            int address = int.Parse(ctrl.Attribute("Address").Value);
            int offset = dataAddresses.ElementAt(0).Value;

            if (ctrl.Attribute("InlineData") != null)
            {
                value = StringToByteArray(ctrl.Attribute("InlineData").Value);
            }
            else
            {
                int size = int.Parse(ctrl.Attribute("Size").Value);
                value = new byte[size];
                for (int i = 0; i < size; i++)
                {
                    try
                    {
                        value[i] = dataMems.Values.ElementAt(0)[address - offset + i];
                    } catch{

                    }
                }
            }

            Debug.WriteLine($"Schreibe Addresse: {address} mit {value.Count()} Bytes");
            await dev.MemoryWrite(address, value);
        }

        private async Task WriteApplication(AppAdditional adds)
        {
            TodoText = "Berechne Speicher...";

            GenerateApplication(adds);

            TodoText = "Schreibe Speicher...";

            switch (_type)
            {
                case ProgAppType.Komplett:
                    foreach (AppSegmentViewModel seg in dataSegs.Values)
                    {
                        await dev.MemoryWrite(seg.Address, dataMems[seg.Id]);
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

            ProjectContext _c = new ProjectContext(SaveHelper.connProject);

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


            Dictionary<int, List<string>> unions = new Dictionary<int, List<string>>();

            foreach (IDynChannel ch in Channels)
            {
                bool vis1 = SaveHelper.CheckConditions(ch.Conditions, Id2Param);

                foreach (ParameterBlock block in ch.Blocks)
                {
                    bool vis2 = SaveHelper.CheckConditions(block.Conditions, Id2Param);

                    foreach (IDynParameter para in block.Parameters)
                    {
                        bool vis3 = SaveHelper.CheckConditions(para.Conditions, Id2Param);


                        AppParameter xpara = AppParas[para.Id];

                        //Wenn Parameter in keiner Union ist zurück geben
                        if(xpara.UnionId == 0)
                        {
                            //TODO prüfen ob auch bei neuen notwendig oder nur Namespace 11
                            if(vis1 && vis2 && vis3)
                            {
                                xpara.Value = para.Value;
                                paras.Add(xpara);
                            }
                        } else
                        {
                            if (!unions.ContainsKey(xpara.UnionId))
                            {
                                unions.Add(xpara.UnionId, new List<string>());
                            }

                            if (vis1 && vis2 && vis3)
                            {
                                if (!paras.Contains(xpara)) // && xpara.Access == AccessType.Full) //IDEE wenn man alle enzeigt hier ebenfalls mitbedenken
                                {
                                    xpara.Value = para.Value;
                                    paras.Add(xpara);
                                }

                                unions[xpara.UnionId].Add(xpara.Id);
                            }

                        }

                        //TODO keine ahnung was das soll
                        //else if (AppParas.Values.Where(p => p.Offset == xpara.Offset).Count() < 2)
                        //{
                        //    //xpara.Value = para.Value;
                        //    paras.Add(xpara);
                        //}
                        //else if (AppParas.Values.Where(p => p.Offset == xpara.Offset && p.Value != xpara.Value).Count() == 0)
                        //{
                        //    paras.Add(xpara);
                        //}
                    }
                }
            }
            
            foreach(KeyValuePair<int, List<string>> union in unions.Where(x => x.Value.Count == 0))
            {
                if(AppParas.Values.Any(p => p.UnionId == union.Key && p.UnionDefault))
                {
                    if(AppParas.Values.Where(p => p.UnionId == union.Key && p.UnionDefault).Count() > 1)
                    {
                        Log.Error("Applikation enthält mehrere DefaultUnionParameter: Id = " + union.Key);
                    }
                    AppParameter para = AppParas.Values.First(p => p.UnionId == union.Key && p.UnionDefault);
                    paras.Add(para);
                }
            }


            return paras;
        }

        private async Task AllocRelSegment(AppAdditional adds, XElement ctrl)
        {
            string LsmId = ctrl.Attribute("LsmIdx").Value;
            //switch (LsmId)
            //{
            //    case "1":
            //        GenerateGroupTable();
            //        length = dataGroupTable.Count;
            //        break;
            //    case "2":
            //        GenerateAssoTable();
            //        length = dataAssoTable.Count;
            //        break;
            //    case "3":
            //        GenerateApplication(adds);
            //        length = dataMems.Values.ElementAt(0).Length;
            //        break;
            //}


            //byte[] tempBytes;
            List<byte> data = new List<byte>() { 0x03, 0x0b };

            byte[] tempBytes = BitConverter.GetBytes(Convert.ToUInt32(ctrl.Attribute("Size").Value));
            data.AddRange(tempBytes.Reverse()); // Length

            byte mode = (byte)(ctrl.Attribute("Mode").Value == "1" ? 0x01 : 0x00);
            byte fill = byte.Parse(ctrl.Attribute("Fill").Value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
            data.AddRange(new byte[] { mode, fill, 0x00, 0x00 });

            await dev.PropertyWrite(Convert.ToByte(LsmId), 5, data.ToArray(), true);
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
                    int manu = int.Parse(appid[1].Substring(0, 4), System.Globalization.NumberStyles.HexNumber);

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

            await dev.MemoryWrite(260, data.ToArray());

            byte[] data2 = new byte[] { 0xFF };
            try
            {
                data2 = await dev.MemoryRead(46825 + int.Parse(LsmId), 1);
            }
            catch { }

            Dictionary<int, byte> map = new Dictionary<int, byte>() { { 4, 0x00 }, { 3, 0x02 }, { 2, 0x02 }, { 1, 0x02 } };
            if (data2[0] != map[int.Parse(LsmId)])
            {
                if (counter > 2)
                {
                    Debug.WriteLine("Fehlgeschlagen!");
                    Log.Error($"AllocSegment fehlgeschlagen: Idx = {lsmIdx}, Response = {Convert.ToString(data2)}");
                }
                else
                    await AllocSegment(ctrl, segType, counter + 1);
            }
        }

        private async Task WriteTableGroup(int addr)
        {
            TodoText = "Schreibe Gruppentabelle...";

            //länge der Tabelle erstmal auf 1 setzen
            Debug.WriteLine("Tabelle auf 1");
            await dev.MemoryWrite(addr, new byte[] { 0x01 });

            //Gruppenadressen in Tabelle eintragen
            GenerateGroupTable();
            Debug.WriteLine("Tabelle schreiben");
            Log.Information($"Gruppentabelle mit {addedGroups.Count} einträgen schreiben");
            await dev.MemoryWrite(addr + 3, dataGroupTable.ToArray());

            await Task.Delay(100);

            Debug.WriteLine("Tabelle länge setzen");
            await dev.MemoryWrite(addr, new byte[] { Convert.ToByte(addedGroups.Count) });
            Log.Information($"Gruppentabelle fertig");


            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                Device.LoadedGroup = true;
            });
        }

        private async Task WriteAssociationTable(int addr)
        {
            TodoText = "Schreibe Assoziationstabelle...";

            //Setze länge der Tabelle auf 0
            await dev.MemoryWrite(addr, new byte[] { 0x00 });

            //Schreibe Assoziationstabelle in Speicher
            GenerateAssoTable();
            Log.Information($"Assoziationstabelle mit {dataAddresses.Count / 2} einträgen schreiben");
            await dev.MemoryWrite(addr + 1, dataAssoTable.ToArray());

            //Setze Länge der Tabelle
            await dev.MemoryWrite(addr, new byte[] { Convert.ToByte(dataAssoTable.Count / 2) });
            Log.Information("Assoziationstabelle fertig");

            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                Device.LoadedGroup = true;
            });
        }

        private async Task LsmState(XElement ctrl, int counter = 0)
        {
            byte[] data = new byte[app.IsRelativeSegment ? 10 : 11];
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


            if (app.IsRelativeSegment)
            {
                data[0] = Convert.ToByte(state);
                await dev.PropertyWrite(BitConverter.GetBytes(lsmIdx)[0], 5, data);
                data = null;
            }
            else
            {
                int endU = (lsmIdx << 4) | state;
                data[0] = Convert.ToByte(endU);
                await dev.MemoryWrite(260, data);
                await Task.Delay(50);
                data = await dev.MemoryRead(46825 + lsmIdx, 1);
            }

            Dictionary<int, byte> map = new Dictionary<int, byte>() { { 4, 0x00 }, { 3, 0x02 }, { 2, 0x01 }, { 1, 0x02 } };
            if (data != null && data[0] != map[(int)state])
            {
                if (counter > 2)
                {
                    Debug.WriteLine("Fehlgeschlagen!");
                    Log.Error($"LsmState fehlgeschlagen: Idx = {lsmIdx}, State = {state}, Response = {Convert.ToString(data)}");
                }
                else
                    await LsmState(ctrl, counter + 1);
            }
        }

        private void GenerateGroupTable()
        {
            addedGroups = new List<string> { "" };
            foreach (DeviceComObject com in Device.ComObjects)
                foreach (FunctionGroup group in com.Groups)
                    if (!addedGroups.Contains(group.Address.ToString()))
                        addedGroups.Add(group.Address.ToString());
            if (addedGroups.Count > app.Table_Group_Max)
            {
                Log.Error("Die Applikation erlaubt nur " + app.Table_Group_Max + " Gruppenverbindungen. Verwendet werden " + addedGroups.Count + ".");
                throw new Exception("Die Applikation erlaubt nur " + app.Table_Group_Max + " Gruppenverbindungen. Verwendet werden " + addedGroups.Count + ".");
            }

            addedGroups.Sort();

            dataGroupTable = new List<byte>();
            foreach (string group in addedGroups) //Liste zum Datenpaket hinzufügen
                if (group != "") dataGroupTable.AddRange(MulticastAddress.FromString(group).GetBytes());
        }


        private void GenerateAssoTable()
        {
            //Erstelle Assoziationstabelle
            dataAssoTable = new List<byte>();

            Dictionary<byte, List<byte>> Table = new Dictionary<byte, List<byte>>();

            foreach (DeviceComObject com in Device.ComObjects)
            {
                foreach (FunctionGroup group in com.Groups)
                {
                    int indexG = addedGroups.IndexOf(group.Address.ToString());
                    int indexC = com.Number;

                    byte bIndexG = BitConverter.GetBytes(indexG)[0];
                    byte bIndexC = BitConverter.GetBytes(indexC)[0];

                    if (!Table.ContainsKey(bIndexG))
                        Table.Add(bIndexG, new List<byte>());

                    Table[bIndexG].Add(bIndexC);
                }
            }

            foreach (KeyValuePair<byte, List<byte>> val in Table.OrderBy(kpv => kpv.Key))
            {
                val.Value.Sort();
                foreach (byte index in val.Value)
                {
                    dataAssoTable.Add(val.Key);
                    dataAssoTable.Add(index);
                }
            }

            if ((dataAssoTable.Count / 2) > app.Table_Assosiations_Max)
            {
                Log.Error("Die Applikation erlaubt nur " + app.Table_Assosiations_Max + " Assoziationsverbindungen. Verwendet werden " + (dataAssoTable.Count / 2) + ".");
                throw new Exception("Die Applikation erlaubt nur " + app.Table_Assosiations_Max + " Assoziationsverbindungen. Verwendet werden " + (dataAssoTable.Count / 2) + ".");
            }
        }

        /*
        private void GenerateApplication(AppAdditional adds)
        {
            Dictionary<string, AppParameter> paras = new Dictionary<string, AppParameter>();
            Dictionary<string, AppParameterTypeViewModel> types = new Dictionary<string, AppParameterTypeViewModel>();
            List<int> changed = new List<int>();

            foreach (AppParameter para in GetVisibleParams(adds))
                paras.Add(para.Id, para);

            foreach (AppParameterTypeViewModel type in _context.AppParameterTypes)
                types.Add(type.Id, type);

            foreach (AppParameter para in paras.Values)
            {
                if (para.SegmentId == null) continue;
                if (!dataSegs.ContainsKey(para.SegmentId))
                {
                    AppSegmentViewModel seg = _context.AppSegments.Single(a => a.Id == para.SegmentId);
                    if (seg.Data == null)
                    {
                        dataMems[para.SegmentId] = new byte[seg.Size];
                    }
                    else
                    {
                        dataMems[para.SegmentId] = Convert.FromBase64String(seg.Data);
                    }

                    dataSegs[para.SegmentId] = seg;
                    dataAddresses.Add(seg.Id, seg.Address);
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
                        Encoding.UTF8.GetBytes(para.Value).CopyTo(paraData, 0);
                        break;
                }



                if (type.Size >= 8)
                {
                    byte[] memory = dataMems[para.SegmentId];
                    for (int i = 0; i < type.Size / 8; i++)
                    {
                        memory[para.Offset + i] = paraData[i];
                        changed.Add(para.Offset + i);
                    }
                }
            }

            int unionId = 1;
            while (true)
            {
                if (!_context.AppParameters.Any(p => p.ApplicationId == adds.Id && p.UnionId == unionId)) break;

                bool flag = false;
                foreach (AppParameter para in _context.AppParameters.Where(p => p.ApplicationId == adds.Id && p.UnionId == unionId))
                {
                    if (paras.ContainsKey(para.Id))
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag && _context.AppParameters.Any(p => p.ApplicationId == adds.Id && p.UnionId == unionId && p.UnionDefault))
                {
                    AppParameter defPara = _context.AppParameters.First(p => p.ApplicationId == adds.Id && p.UnionId == unionId && p.UnionDefault);

                    AppParameterTypeViewModel type = types[defPara.ParameterTypeId];

                    byte[] paraData = type.Size >= 8 ? new byte[type.Size / 8] : new byte[1];

                    switch (type.Type)
                    {
                        case ParamTypes.Enum:
                        case ParamTypes.NumberUInt:
                            paraData = BitConverter.GetBytes(Convert.ToUInt32(defPara.Value)).Take(type.Size / 8).ToArray();
                            Array.Reverse(paraData);
                            break;
                        case ParamTypes.NumberInt:
                            paraData = BitConverter.GetBytes(Convert.ToInt32(defPara.Value)).Take(type.Size / 8).ToArray();
                            Array.Reverse(paraData);
                            break;

                        case ParamTypes.Text:
                            paraData = Encoding.UTF8.GetBytes(defPara.Value);
                            break;
                    }



                    if (type.Size >= 8)
                    {
                        byte[] memory = dataMems[defPara.SegmentId];
                        for (int i = 0; i < type.Size / 8; i++)
                        {
                            memory[defPara.Offset + i] = paraData[i];
                            changed.Add(defPara.Offset + i);
                        }
                    }
                }

                unionId++;
            }
        }
        */

        private void GenerateApplication(AppAdditional adds)
        {
            Dictionary<string, AppParameter> paras = new Dictionary<string, AppParameter>();
            Dictionary<string, AppParameterTypeViewModel> types = new Dictionary<string, AppParameterTypeViewModel>();
            List<int> changed = new List<int>();

            foreach (AppParameter para in GetVisibleParams(adds))
                paras.Add(para.Id, para);

            foreach (AppParameterTypeViewModel type in _context.AppParameterTypes)
                types.Add(type.Id, type);

            foreach (AppParameter para in paras.Values)
            {
                if (para.SegmentId == null) continue;
                if (!dataSegs.ContainsKey(para.SegmentId))
                {
                    AppSegmentViewModel seg = _context.AppSegments.Single(a => a.Id == para.SegmentId);
                    if (seg.Data == null)
                    {
                        dataMems[para.SegmentId] = new byte[seg.Size];
                    }
                    else
                    {
                        dataMems[para.SegmentId] = Convert.FromBase64String(seg.Data);
                    }

                    dataSegs[para.SegmentId] = seg;
                    dataAddresses.Add(seg.Id, seg.Address);
                }

                AppParameterTypeViewModel type = types[para.ParameterTypeId];

                int paraDataLength = type.Size >= 8 ? (type.Size / 8) : 1;
                byte[] paraData = new byte[paraDataLength];

                switch (type.Type)
                {
                    case ParamTypes.Enum:
                    case ParamTypes.NumberUInt:
                        paraData = BitConverter.GetBytes(Convert.ToUInt32(para.Value)).Take(paraDataLength).ToArray();
                        Array.Reverse(paraData);
                        break;
                    case ParamTypes.NumberInt:
                        paraData = BitConverter.GetBytes(Convert.ToInt32(para.Value)).Take(paraDataLength).ToArray();
                        Array.Reverse(paraData);
                        break;

                    case ParamTypes.Text:
                        Encoding.UTF8.GetBytes(para.Value).CopyTo(paraData, 0);
                        break;
                    default:
                        Log.Error("Parametertyp wurde noch nicht beim Applikation schreiben eingepflegt... Typ: " + type.Type.ToString());
                        throw new Exception("Unbekannter ParaTyp: " + type.Type.ToString());
                }




                if (type.Size >= 8)
                {
                    Debug.WriteLine(para.Offset);
                    byte[] memory = dataMems[para.SegmentId];
                    for (int i = 0; i < type.Size / 8; i++)
                    {
                        memory[para.Offset + i] = paraData[i];
                        changed.Add(para.Offset + i);
                    }
                } else
                {
                    List<int> masks = new List<int>() { 0b00000001, 0b00000011, 0b00000111, 0b00001111, 0b00011111, 0b00111111, 0b01111111 };
                    int dataByte = paraData[0];

                    int mask = masks[type.Size - 1];
                    dataByte &= mask;
                    dataByte = dataByte << para.OffsetBit;

                    int memByte = dataMems[para.SegmentId][para.Offset];
                    memByte = ((mask << para.OffsetBit) ^ 255) & memByte;
                    memByte |= dataByte;
                    dataMems[para.SegmentId][para.Offset] = Convert.ToByte(memByte);
                }
            }

            int unionId = 1;
            while (true)
            {
                if (!_context.AppParameters.Any(p => p.ApplicationId == adds.Id && p.UnionId == unionId)) break;

                bool flag = false;
                foreach (AppParameter para in _context.AppParameters.Where(p => p.ApplicationId == adds.Id && p.UnionId == unionId))
                {
                    if (paras.ContainsKey(para.Id))
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag && _context.AppParameters.Any(p => p.ApplicationId == adds.Id && p.UnionId == unionId && p.UnionDefault))
                {
                    AppParameter defPara = _context.AppParameters.First(p => p.ApplicationId == adds.Id && p.UnionId == unionId && p.UnionDefault);

                    AppParameterTypeViewModel type = types[defPara.ParameterTypeId];

                    byte[] paraData = type.Size >= 8 ? new byte[type.Size / 8] : new byte[1];

                    switch (type.Type)
                    {
                        case ParamTypes.Enum:
                        case ParamTypes.NumberUInt:
                            paraData = BitConverter.GetBytes(Convert.ToUInt32(defPara.Value)).Take(type.Size / 8).ToArray();
                            Array.Reverse(paraData);
                            break;
                        case ParamTypes.NumberInt:
                            paraData = BitConverter.GetBytes(Convert.ToInt32(defPara.Value)).Take(type.Size / 8).ToArray();
                            Array.Reverse(paraData);
                            break;

                        case ParamTypes.Text:
                            paraData = Encoding.UTF8.GetBytes(defPara.Value);
                            break;
                    }



                    if (type.Size >= 8)
                    {
                        byte[] memory = dataMems[defPara.SegmentId];
                        for (int i = 0; i < type.Size / 8; i++)
                        {
                            memory[defPara.Offset + i] = paraData[i];
                            changed.Add(defPara.Offset + i);
                        }
                    } else
                    {

                    }
                }

                unionId++;
            }
        }



        private static byte[] StringToByteArray(string str)
        {
            Dictionary<string, byte> hexindex = new Dictionary<string, byte>();
            for (int i = 0; i <= 255; i++)
                hexindex.Add(i.ToString("X2"), (byte)i);

            List<byte> hexres = new List<byte>();
            for (int i = 0; i < str.Length; i += 2)
                hexres.Add(hexindex[str.Substring(i, 2)]);

            return hexres.ToArray();
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

        public enum ProcedureTypes
        {
            Load,
            Unload
        }

    }
}
