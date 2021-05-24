using Kaenx.Classes.Buildings;
using Kaenx.Classes.Dynamic;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Connections;
using Kaenx.Konnect.Messages.Response;
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
        private List<int> addedGroups;
        private CatalogContext _context = new CatalogContext();


        // tables are the complete image including preceding length/unicast address
        private List<byte> dataComObjectTable = new List<byte>();
        private List<byte> dataGroupTable = new List<byte>();
        private bool useLongAssociations = false;
        private List<byte> dataAssoTable = new List<byte>();
        private Dictionary<string, AppSegmentViewModel> dataSegs = new Dictionary<string, AppSegmentViewModel>();
        private Dictionary<string, byte[]> dataMems = new Dictionary<string, byte[]>();
        private Dictionary<string, int> dataAddresses = new Dictionary<string, int>();

        /// <summary>
        /// Temporary for Unit test. If not null overrides ApplicationParams
        /// </summary>
        public List<AppParameter> OverrideVisibleParams = null;

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

        private string ProcedureSubType => _type switch
        {
            ProgAppType.Komplett => "all",
            ProgAppType.Partiell when !Device.LoadedApplication && !Device.LoadedGroup => "par,grp",
            ProgAppType.Partiell when !Device.LoadedApplication => "par",
            ProgAppType.Partiell when !Device.LoadedGroup => "grp",
            ProgAppType.Partiell => "cfg",
            ProgAppType.Minimal => throw new NotImplementedException("Minimal Programmieren"),
            _ => throw new InvalidOperationException("Unreachable"),
        };

        public UnloadHelper Helper;
        public IKnxConnection Connection { get; set; }
        public CatalogContext Context { get => _context; set => _context = value; }

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


            _ = Start();
        }


        private async Task Start()
        {
            dev = new BusDevice(Device.LineName, Connection);
            TodoText = ProcedureType == ProcedureTypes.Load ? "Applikation schreiben" : "Gerät entladen";


            if (ProcedureType == ProcedureTypes.Load || (Helper != null && (Helper.UnloadApplication || Helper.UnloadBoth)))
            {
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
                        procedure = temp.Descendants(XName.Get("Procedure", temp.Name.NamespaceName)).Single(m => m.Attribute("ProcedureType").Value == procedureType && m.Attribute("ProcedureSubType").Value == ProcedureSubType);
                        break;

                    case LoadProcedureTypes.Merge:
                        XElement temp2 = XDocument.Parse(Encoding.UTF8.GetString(adds.LoadProcedures)).Root;
                        temp = await GetKnxMaster();
                        temp = temp.Descendants(XName.Get("MaskVersion", temp.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == app.Mask);
                        temp = temp.Descendants(XName.Get("Procedure", temp.Name.NamespaceName)).First(m => m.Attribute("ProcedureType").Value == procedureType && m.Attribute("ProcedureSubType").Value == ProcedureSubType);

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

                XNamespace kaenxNS = XNamespace.Get("https://github.com/thewhobox/Kaenx");
                XElement preDownloadChecks = new XElement(kaenxNS + "PreDownloadChecks");
                procedure.Elements(procedure.GetDefaultNamespace() + "LdCtrlConnect").First().AddAfterSelf(preDownloadChecks);
                XElement generateImage = new XElement(kaenxNS + "GenerateImage");
                preDownloadChecks.AddAfterSelf(generateImage);

                if (true) //TODO: if !device.HasApplicationProgram2
                {
                    List<XElement> toRemove = new List<XElement>();
                    foreach (XElement ctrl in procedure.Elements())
                    {
                        switch (ctrl.Name.LocalName)
                        {
                            case "LdCtrlUnload":
                                if (ctrl.Attribute("LsmIdx").Value == "5")
                                    ctrl.SetAttributeValue("OnError", "Ignore");
                                break;
                            case "LdCtrlLoad":
                            case "LdCtrlLoadCompleted":
                                if (ctrl.Attribute("LsmIdx").Value == "5")
                                    toRemove.Add(ctrl);
                                break;
                            case "LdCtrlWriteProp":
                                if (ctrl.Attribute("ObjIdx").Value == "5")
                                    toRemove.Add(ctrl);
                                break;
                        }
                    }
                    foreach (XElement ctrl in toRemove)
                        ctrl.Remove();
                }

                foreach (XElement ctrl in procedure
                    .Elements(procedure.GetDefaultNamespace() + "LdCtrlWriteProp")
                    .Where(ctrl => ctrl.Attribute("ObjIdx").Value == "4" && ctrl.Attribute("PropId").Value == "13"))
                {
                    ctrl.Attribute("InlineData").Value = $"{app.Manufacturer:X4}{app.Number:X4}{app.Version:X2}";
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
                                await dev.Disconnect();
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
                                    await dev.Disconnect();
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
                                break;

                            case "LdCtrlWriteProp":
                                byte objIdx = Convert.ToByte(ctrl.Attribute("ObjIdx").Value);
                                byte propId = Convert.ToByte(ctrl.Attribute("PropId").Value);
                                //Parse Hexstring
                                string inlineData = ctrl.Attribute("InlineData").Value;
                                byte[] data = new byte[inlineData.Length / 2];
                                for (var i = 0; i < data.Length; i++)
                                    data[i] = Convert.ToByte(inlineData.Substring(i * 2, 2), 16);

                                byte[] response = await dev.PropertyWriteResponse<byte[]>(objIdx, propId, data);
                                if (ctrl.Attribute("Verify").Value == "true" && !data.SequenceEqual(response))
                                    throw new Exception($"PropertyVerify fehlgeschlagen: Objekt: {objIdx}, Property: {propId}, soll: {BitConverter.ToString(data)}, ist: {BitConverter.ToString(response)}");
                                break;

                            case "LdCtrlWriteRelMem":
                                await WriteRelSegment(ctrl);
                                break;

                            case "LdCtrlDelay":
                                int ms = int.Parse(ctrl.Attribute("MilliSeconds").Value);
                                await Task.Delay(ms);
                                break;

                            case "LdCtrlWriteMem":
                                await WriteMemory(adds, ctrl);
                                break;

                            case "PreDownloadChecks":
                                await PreDownloadChecks();
                                break;

                            case "GenerateImage":
                                GenerateImage(adds);
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



            if (Helper != null && (Helper.UnloadAddress || Helper.UnloadBoth))
            {
                await Task.Delay(1000); //warten, da sonst DisconnectResp erst nach dem Connect kommt!
                await dev.Connect(true);
                string mask = await dev.DeviceDescriptorRead();
                mask = "MV-" + mask;
                await dev.RessourceWrite("ProgrammingMode", new byte[] { 0x01 });
                await dev.Disconnect();
                BusCommon comm = new BusCommon(Connection);
                comm.IndividualAddressWrite(UnicastAddress.FromString("15.15.255"));
                await Task.Delay(200);
                BusDevice dev2 = new BusDevice("15.15.255", Connection);
                await dev2.Connect();
                await dev2.Restart();
                await dev2.Disconnect();
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
                        await WriteApplication();
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
            dataGroupTable.CopyTo(dataMems[app.Table_Group], app.Table_Group_Offset);
            dataAssoTable.CopyTo(dataMems[app.Table_Assosiations], app.Table_Assosiations_Offset);

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
                    }
                    catch
                    {

                    }
                }
            }

            Debug.WriteLine($"Schreibe Addresse: {address} mit {value.Count()} Bytes");
            await dev.MemoryWrite(address, value);
        }

        private async Task PreDownloadChecks()
        {
            await dev.DeviceDescriptorRead();
            ushort databaseMask = ushort.Parse(app.Mask.Replace("MV-", ""), System.Globalization.NumberStyles.HexNumber);
            if (databaseMask != dev.MaskVersion)
                throw new Exception($"Maskenversion im Gerät ({dev.MaskVersion:X4}) stimmt nicht mit der Produktdatenbank ({databaseMask:X4}) überein");
            //Medium independant
            int maskVersion = (ushort)dev.MaskVersion & 0x0fff;
            await dev.ReadMaxAPDULength();

            XNamespace ns = dev.MaskXML.GetDefaultNamespace();
            bool supportsAuthorize = dev.MaskXML.Element(ns + "HawkConfigurationData").Element(ns + "Features")
                .Elements(ns + "Feature").Any(feature => feature.Attribute("Name").Value == "AuthorizeLevels");
            if (supportsAuthorize)
            {
                if (await dev.Authorize(0xffffffff) != 0)
                    throw new Exception("Standardpasswort wurde vom Gerät nicht akzeptiert");
            }

            bool programGroupObjects = _type == ProgAppType.Komplett || !Device.LoadedGroup;
            if (maskVersion == 0x07B0 && programGroupObjects)
            {
                const byte ASSOCIATION_TABLE = 2;
                const byte PID_TABLE = 23;
                MsgPropertyDescriptionRes description = await dev.PropertyDescriptionRead(ASSOCIATION_TABLE, PID_TABLE);
                useLongAssociations = description.Type switch
                {
                    18 => false, // PDT_GENERIC_02
                    20 => true, // PDT_GENERIC_04
                    _ => throw new Exception($"Datentyp von Assoziazionstabelle kann nicht PDT-{description.Type} sein"),
                };
            }

            if (_type == ProgAppType.Komplett)
            {
                if (maskVersion != 0x0701 && !(0x0910 <= maskVersion && maskVersion <= 0x091f))
                {
                    int deviceManufacturer = await dev.RessourceRead<int>("DeviceManufacturerId");
                    if (app.Manufacturer != deviceManufacturer)
                        throw new Exception("Hersteller des Gerätes ist nicht gleich mit der Produktdatenbank");
                }
                //TODO: PortADDR for BCU1 (Memory: 010C) and BCU2 (PID_PORT_CONFIGURATION in device object)
                // Compare to download image 010C if this address is included
                if (maskVersion == 0x07B0)
                {
                    const byte PID_ORDER_INFO = 15;
                    const byte PID_VERSION = 25;
                    const byte PID_HARDWARE_TYPE = 78;
                    await dev.PropertyRead(0, PID_VERSION);
                    await dev.PropertyRead(0, PID_HARDWARE_TYPE);
                    await dev.PropertyRead(0, PID_ORDER_INFO);
                    //TODO: Compare if properties are included in download image 
                    // KNX suggests an unused ParameterTypeRestriction with a default including BinaryValue = Value to compare, and a Property Parameter with respective objIdx and propId
                    // Compare PID_ORDER_INFO and PID_HARDWARE_TYPE with ==, PID_VERSION with >= according to DPT 217.001
                }
            }
            else if (_type == ProgAppType.Partiell)
            {
                // if has load state machine: PID_LOAD_STATE_CONTROL of ApplicationProgram must be loaded
                if (maskVersion < 0x0910 || 0x091f < maskVersion)
                {
                    // ApplicationId must be equal to ProductDatabase
                }

                if (maskVersion == 0x0701 || maskVersion == 0x0705 || maskVersion == 0x07B0)
                {
                    if (false) // Has ApplicationProgram2
                    {
                        // PID_LOAD_STATE_CONTROL of InterfaceProgram must be loaded
                        // InterfaceProgramId must be equal to ProductDatabase
                    }
                    else
                    {
                        // PID_LOAD_STATE_CONTROL of InterfaceProgram must be unloaded
                    }
                }
                throw new NotImplementedException();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void GenerateImage(AppAdditional adds)
        {
            GenerateComObjectTable();
            GenerateGroupTable();
            GenerateAssoTable();
            GenerateApplication(adds);
        }

        private async Task WriteApplication()
        {
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


                        if (para is ParamSeperator || para is ParamSeperatorBox) continue;
                        AppParameter xpara = AppParas[para.Id];

                        //Wenn Parameter in keiner Union ist zurück geben
                        if (xpara.UnionId == 0)
                        {
                            //TODO prüfen ob auch bei neuen notwendig oder nur Namespace 11
                            if (vis1 && vis2 && vis3)
                            {
                                xpara.Value = para.Value;
                                paras.Add(xpara);
                            }
                        }
                        else
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

            foreach (KeyValuePair<int, List<string>> union in unions.Where(x => x.Value.Count == 0))
            {
                if (AppParas.Values.Any(p => p.UnionId == union.Key && p.UnionDefault))
                {
                    if (AppParas.Values.Where(p => p.UnionId == union.Key && p.UnionDefault).Count() > 1)
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
            byte lsmId = byte.Parse(ctrl.Attribute("LsmIdx").Value);
            int size = int.Parse(ctrl.Attribute("Size").Value);
            if (size == 2)
            {
                switch (lsmId)
                {
                    case 1: size = dataGroupTable.Count; break;
                    case 2: size = dataAssoTable.Count; break;
                    case 3: size = dataComObjectTable.Count; break;
                    case 4: size = dataMems.Values.ElementAt(0).Length; break;
                }
            }

            List<byte> extraData = new List<byte>();
            extraData.Add(0x0b);
            byte[] sizeBytes = BitConverter.GetBytes(size);
            extraData.AddRange(sizeBytes.Reverse());
            extraData.Add(ctrl.Attribute("Mode").Value == "1" ? (byte)0x01 : (byte)0x00);
            extraData.Add(byte.Parse(ctrl.Attribute("Fill").Value));

            await SendLsmEvent(lsmId, 0x03, 0x02, extraData: extraData.ToArray());
        }

        private async Task WriteRelSegment(XElement ctrl)
        {
            byte objIdx = byte.Parse(ctrl.Attribute("ObjIdx").Value);
            int offset = int.Parse(ctrl.Attribute("Offset").Value);
            int size = int.Parse(ctrl.Attribute("Size").Value);
            bool verify = bool.Parse(ctrl.Attribute("Verify").Value);

            ICollection<byte> data = objIdx switch
            {
                1 => dataGroupTable,
                2 => dataAssoTable,
                3 => dataComObjectTable,
                _ => null,
            };
            if (data != null && offset == 0 && size == 0x100000)
                size = data.Count;

            if (data == null)
            {
                AppSegmentViewModel seg = dataSegs.Values.Single(seg => seg.LsmId == objIdx);
                data = dataMems[seg.Id];
            }

            int address = await dev.PropertyRead<int>(objIdx, 7);
            if (address == 0)
                throw new Exception("Allocation failed");
            await dev.MemoryWrite(address + offset, data.Skip(offset).Take(size).ToArray(), verify);
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

            try
            {
                await dev.MemoryWrite(260, data.ToArray());
            }
            catch (Exception ex)
            {

            }

            byte[] data2 = new byte[] { 0xFF };
            try
            {
                data2 = await dev.MemoryRead(46825 + int.Parse(LsmId), 1);
            }
            catch
            {
            }

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
            Debug.WriteLine("Tabelle schreiben");
            Log.Information($"Gruppentabelle mit {addedGroups.Count} einträgen schreiben");
            await dev.MemoryWrite(addr + 3, dataGroupTable.Skip(3).ToArray());

            await Task.Delay(100);

            Debug.WriteLine("Tabelle länge setzen");
            await dev.MemoryWrite(addr, new byte[] { dataGroupTable[0] });
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
            Log.Information($"Assoziationstabelle mit {dataAssoTable[0]} einträgen schreiben");
            await dev.MemoryWrite(addr + 1, dataAssoTable.Skip(1).ToArray());

            //Setze Länge der Tabelle
            await dev.MemoryWrite(addr, new byte[] { dataAssoTable[0] });
            Log.Information("Assoziationstabelle fertig");

            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                Device.LoadedGroup = true;
            });
        }

        private async Task LsmState(XElement ctrl, int counter = 0)
        {
            XNamespace ns = dev.MaskXML.GetDefaultNamespace();
            XElement propertyLsmFeature = dev.MaskXML.Element(ns + "HawkConfigurationData").Element(ns + "Features").Elements().FirstOrDefault(x => x.Attribute("Name").Value == "PropertyMappedLsms");
            bool useProperty = propertyLsmFeature != null && propertyLsmFeature.Attribute("Value").Value == "1";

            byte[] data = new byte[useProperty ? 10 : 11];
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


            if (useProperty)
            {
                data[0] = Convert.ToByte(state);
                await dev.PropertyWrite(BitConverter.GetBytes(lsmIdx)[0], 5, data, true);
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

        private async Task SendLsmEvent(byte lsmIndex, byte loadEvent, byte requiredState, int intermediateState = -1, byte[] extraData = null)
        {
            XNamespace ns = dev.MaskXML.GetDefaultNamespace();
            XElement propertyLsmFeature = dev.MaskXML.Element(ns + "HawkConfigurationData").Element(ns + "Features").Elements().FirstOrDefault(x => x.Attribute("Name").Value == "PropertyMappedLsms");
            bool useProperty = propertyLsmFeature != null && propertyLsmFeature.Attribute("Value").Value == "1";

            byte[] data = new byte[useProperty ? 10 : 11];
            data[0] = loadEvent;
            if (extraData != null)
                extraData.CopyTo(data, 1);

            byte lsmState;
            if (useProperty)
            {
                lsmState = (await dev.PropertyWriteResponse(lsmIndex, 5, data))[0];
            }
            else
            {
                data[0] |= (byte)(lsmIndex << 4);
                await dev.MemoryWrite(260, data);
                await Task.Delay(50);
                lsmState = (await dev.MemoryRead(46825 + lsmIndex, 1))[0];
            }

            while (lsmState == intermediateState)
            {
                if (useProperty)
                    lsmState = (await dev.PropertyRead(lsmIndex, 5))[0];
                else
                    lsmState = (await dev.MemoryRead(46825 + lsmIndex, 1))[0];

                await Task.Delay(50);
            }

            if (lsmState != requiredState)
                throw new Exception($"Fehler in LoadSateMachine {lsmIndex}: Status sollte {requiredState} sein, ist aber {lsmState}");
        }

        private void GenerateComObjectTable()
        {
            var strippedComObjects = new List<DeviceComObject>();
            foreach (DeviceComObject com in Device.ComObjects.Where(com => com.IsEnabled))
            {
                int comIndex = com.Number - 1;
                if (comIndex < strippedComObjects.Count)
                {
                    strippedComObjects[comIndex] = com;
                }
                else
                {
                    while (strippedComObjects.Count < comIndex)
                        strippedComObjects.Add(null);
                    strippedComObjects.Add(com);
                }
            }

            dataComObjectTable = new List<byte>();
            //Others are not supported for now
            if ((dev.MaskVersion & 0x0fff) == 0x07B0)
            {
                dataComObjectTable.Add((byte)(strippedComObjects.Count >> 8));
                dataComObjectTable.Add((byte)strippedComObjects.Count);
                foreach (DeviceComObject com in strippedComObjects)
                {
                    if (com == null)
                    {
                        dataComObjectTable.Add(0);
                        dataComObjectTable.Add(0);
                    }
                    else
                    {
                        byte flags = 0;
                        if (com.Flag_Update)
                            flags |= (1 << 7);
                        if (com.Flag_Transmit)
                            flags |= (1 << 6);
                        if (com.Flag_ReadOnInit)
                            flags |= (1 << 5);
                        if (com.Flag_Write)
                            flags |= (1 << 4);
                        if (com.Flag_Read)
                            flags |= (1 << 3);
                        if (com.Flag_Communication)
                            flags |= (1 << 2);
                        const byte PRIORITY_LOW = 0b11;
                        flags |= PRIORITY_LOW;

                        byte fieldType = com.DataPointSubType.SizeInBit switch
                        {
                            0 => 0,
                            1 => 0,
                            2 => 1,
                            3 => 2,
                            4 => 3,
                            5 => 4,
                            6 => 5,
                            7 => 6,
                            1 * 8 => 7,
                            2 * 8 => 8,
                            3 * 8 => 9,
                            4 * 8 => 10,
                            6 * 8 => 11,
                            8 * 8 => 12,
                            10 * 8 => 13,
                            14 * 8 => 14,
                            5 * 8 => 15,
                            7 * 8 => 16,
                            9 * 8 => 17,
                            11 * 8 => 18,
                            12 * 8 => 19,
                            13 * 8 => 20,
                            var x when 15 * 8 < x && x < 248 * 8 => (byte)(x / 8 - 15 + 21),
                            var x => throw new Exception($"Datenpunkte mit {x} bits werden nicht unterstützt"),
                        };
                        dataComObjectTable.Add(flags);
                        dataComObjectTable.Add(fieldType);
                    }
                }
            }
        }

        private void GenerateGroupTable()
        {
            addedGroups = new List<int>();
            foreach (DeviceComObject com in Device.ComObjects)
                foreach (FunctionGroup group in com.Groups)
                    if (!addedGroups.Contains(group.Address.AsUInt16()))
                        addedGroups.Add(group.Address.AsUInt16());

            if (addedGroups.Count > app.Table_Group_Max)
            {
                Log.Error("Die Applikation erlaubt nur " + app.Table_Group_Max + " Gruppenverbindungen. Verwendet werden " + addedGroups.Count + ".");
                throw new Exception("Die Applikation erlaubt nur " + app.Table_Group_Max + " Gruppenverbindungen. Verwendet werden " + addedGroups.Count + ".");
            }

            addedGroups.Sort();

            dataGroupTable = new List<byte>();
            if ((dev.MaskVersion & 0x0fff) == 0x07B0)
            {
                dataGroupTable.Add((byte)(addedGroups.Count >> 8));
                dataGroupTable.Add((byte)addedGroups.Count);
            }
            else
            {
                //Count includes own Physical address
                dataGroupTable.Add((byte)(addedGroups.Count + 1));
                dataGroupTable.AddRange(UnicastAddress.FromString(Device.LineName).GetBytes());
            }
            foreach (int group in addedGroups) //Liste zum Datenpaket hinzufügen
            {
                dataGroupTable.Add((byte)((group & 0xFF00) >> 8));
                dataGroupTable.Add((byte)(group & 0xFF));
            }
        }


        private void GenerateAssoTable()
        {
            dataAssoTable = new List<byte>();

            Dictionary<ushort, List<ushort>> Table = new Dictionary<ushort, List<ushort>>();

            foreach (DeviceComObject com in Device.ComObjects)
            {
                foreach (FunctionGroup group in com.Groups)
                {
                    ushort indexG = (ushort)(addedGroups.IndexOf(group.Address.AsUInt16()) + 1);
                    ushort indexC = (ushort)com.Number;

                    if (!Table.ContainsKey(indexG))
                        Table.Add(indexG, new List<ushort>());

                    Table[indexG].Add(indexC);
                }
            }

            //Reserve Space for count
            dataAssoTable.Add(0);
            if ((dev.MaskVersion & 0x0fff) == 0x07B0)
            {
                dataAssoTable.Add(0);
            }

            int numAssociations = 0;
            foreach (KeyValuePair<ushort, List<ushort>> val in Table.OrderBy(kpv => kpv.Key))
            {
                val.Value.Sort();
                foreach (ushort index in val.Value)
                {
                    numAssociations++;
                    if (useLongAssociations)
                    {
                        dataAssoTable.Add((byte)(val.Key >> 8));
                        dataAssoTable.Add((byte)val.Key);
                        dataAssoTable.Add((byte)(index >> 8));
                        dataAssoTable.Add((byte)index);
                    }
                    else
                    {
                        dataAssoTable.Add((byte)val.Key);
                        dataAssoTable.Add((byte)index);
                    }
                }
            }

            if ((dev.MaskVersion & 0x0fff) == 0x07B0)
            {
                dataAssoTable[0] = (byte)(numAssociations >> 8);
                dataAssoTable[1] = (byte)numAssociations;
            }
            else
            {
                dataAssoTable[0] = (byte)numAssociations;
            }

            if (numAssociations > app.Table_Assosiations_Max)
            {
                Log.Error("Die Applikation erlaubt nur " + app.Table_Assosiations_Max + " Assoziationsverbindungen. Verwendet werden " + numAssociations + ".");
                throw new Exception("Die Applikation erlaubt nur " + app.Table_Assosiations_Max + " Assoziationsverbindungen. Verwendet werden " + numAssociations + ".");
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

            var visibleParams = OverrideVisibleParams ?? GetVisibleParams(adds);
            foreach (AppParameter para in visibleParams)
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
                    case ParamTypes.Time:
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
                }
                else
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
                    }
                    else
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
