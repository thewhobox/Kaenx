using Kaenx.Classes.Buildings;
using Kaenx.Classes.Bus.Data;
using Kaenx.Classes.Dynamic;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Classes;
using Kaenx.View;
using Microsoft.AppCenter.Analytics;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;

namespace Kaenx.Classes.Bus.Actions
{
    public class DeviceConfig : IBusAction, INotifyPropertyChanged
    {
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private DeviceConfigData _data = new DeviceConfigData();
        private Dictionary<string, byte[]> mems = new Dictionary<string, byte[]>();
        private CancellationToken _token;
        private CatalogContext _context = new CatalogContext();
        private BusDevice dev;
        private List<int> connectedCOs = new List<int>();
        private Dictionary<string, string> defParas = new Dictionary<string, string>();
        private Dictionary<int, List<string>> CO2GA = new Dictionary<int, List<string>>();

        public string Type { get; } = "Geräte Konfiguration";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public Connection Connection { get; set; }

        public event IBusAction.ActionFinishedHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;

        public DeviceConfig()
        { 
        }

        public void Run(CancellationToken token)
        {
            _token = token; // TODO implement cancellation
            TodoText = "Verbinden...";

            Start();
        }

        private async void Start()
        {
            dev = new BusDevice(Device.LineName, Connection);
            await dev.Connect();

            #region Grundinfo
            TodoText = "Lese Maskenversion...";

            await Task.Delay(100);

            _data.MaskVersion = "MV-" + await dev.DeviceDescriptorRead();


            if(Device.Serial == null)
            {
                ProgressValue = 10;
                TodoText = "Lese Seriennummer...";
                await Task.Delay(500);

                byte[] serial = new byte[0];
                try
                {
                    serial = await dev.PropertyRead(0, 11);
                    _data.SerialNumber = BitConverter.ToString(serial).Replace("-", "");
                }
                catch (Exception e)
                {
                    _data.SerialNumber = e.Message;
                    Log.Error(e, "Fehler beim holen der Seirennummer");
                }

                if (serial.Length > 0)
                {
                    _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Device.Serial = serial;
                        SaveHelper.UpdateDevice(Device);
                    });
                }
            }


            ProgressValue = 20;
            TodoText = "Lese Applikations Id...";
            await Task.Delay(500);
            string appId = await dev.PropertyRead<string>(_data.MaskVersion, "ApplicationId");
            if (appId.Length == 8) appId = "00" + appId;
            appId = "M-" + appId.Substring(0, 4) + "_A-" + appId.Substring(4, 4) + "-" + appId.Substring(8, 2);

            CatalogContext context = new CatalogContext();

            XElement master = (await GetKnxMaster()).Root;
            XElement manu = master.Descendants(XName.Get("Manufacturer", master.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == appId.Substring(0, 6));
            _data.Manufacturer = manu.Attribute("Name").Value;
            Hardware2AppModel h2a = null;

            try
            {
                h2a = context.Hardware2App.First(h => h.ApplicationId == appId);
                DeviceViewModel dvm = context.Devices.First(d => d.HardwareId == h2a.HardwareId);
                _data.ApplicationName = h2a.Name + " " + h2a.VersionString;
                _data.ApplicationId = h2a.ApplicationId;
            }
            catch { }

            if(h2a == null)
            {
                Finish("Applikation ist nicht im Katalog!");
                return;
            }

            if(Device.ApplicationId != _data.ApplicationId)
            {
                System.Diagnostics.Debug.WriteLine("Jetzt");
                _= App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                    ViewHelper.Instance.ShowNotification("main", $"Applikation für '{Device.LineName}-{Device.Name}' wurde dem physischen Gerät angepasst.", 4000, ViewHelper.MessageType.Info);
                });
                Device.ApplicationId = _data.ApplicationId;
            }


            #endregion

            ProgressValue = 30;
            TodoText = "Lese Assoziationstabelle...";
            await Task.Delay(500);

            ApplicationViewModel appModel = null;

            if (context.Applications.Any(a => a.Id == appId))
            {
                appModel = context.Applications.Single(a => a.Id == appId);
            }

            if(appModel != null)
            {
                List<string> addresses = new List<string>();
                if (!string.IsNullOrEmpty(appModel.Table_Group))
                {
                    AppSegmentViewModel segmentModel = context.AppSegments.Single(s => s.Id == appModel.Table_Group);
                    int groupAddr = segmentModel.Address;
                    byte[] datax = await dev.MemoryRead(groupAddr, 1);

                    int length = Convert.ToInt16(datax[0]) - 1;
                    datax = await dev.MemoryRead(groupAddr + 3, length * 2);

                    for (int i = 0; i < (datax.Length / 2); i++)
                    {
                        int offset = i * 2;
                        addresses.Add(MulticastAddress.FromByteArray(new byte[] { datax[offset], datax[offset + 1] }).ToString());
                    }
                }
                
                if (!string.IsNullOrEmpty(appModel.Table_Assosiations))
                {
                    AppSegmentViewModel segmentModel = context.AppSegments.Single(s => s.Id == appModel.Table_Assosiations);
                    int assoAddr = segmentModel.Address;

                    byte[] datax = await dev.MemoryRead(assoAddr, 1);
                    if (datax.Length > 0)
                    {
                        int length = Convert.ToInt16(datax[0]);

                        datax = await dev.MemoryRead(assoAddr + 1, length * 2);
                        List<AssociationHelper> table = new List<AssociationHelper>();
                        for (int i = 0; i < length; i++)
                        {
                            int offset = i * 2;
                            int grpIndex = datax[offset];
                            int COnr = datax[offset + 1];
                            string grp = addresses[grpIndex-1];

                            if (!CO2GA.ContainsKey(COnr))
                                CO2GA.Add(COnr, new List<string>());

                            if (!CO2GA[COnr].Contains(grp))
                                CO2GA[COnr].Add(grp);

                            if (!connectedCOs.Contains(COnr))
                                connectedCOs.Add(COnr);
                        }
                    }
                }
            }


            ProgressValue = 40;
            TodoText = "Berechne Konfiguration...";

            GetConfig(h2a);
        }



        private async void GetConfig(Hardware2AppModel h2a)
        {
            Dictionary<string, AppParameter> paras = new Dictionary<string, AppParameter>();
            Dictionary<string, AppParameterTypeViewModel> types = new Dictionary<string, AppParameterTypeViewModel>();

            foreach (AppParameter param in _context.AppParameters.Where(p => p.ApplicationId == h2a.ApplicationId))
            {
                paras.Add(param.Id, param);
                defParas.Add(param.Id, param.Value);
            }

            AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == h2a.ApplicationId);

            foreach(AppParameter para in paras.Values)
            {
                AppParameterTypeViewModel paraT;
                if (types.ContainsKey(para.ParameterTypeId))
                    paraT = types[para.ParameterTypeId];
                else
                {
                    paraT = _context.AppParameterTypes.Single(t => t.Id == para.ParameterTypeId);
                    types.Add(paraT.Id, paraT);
                }
                if (paraT.Size == 0) continue;

                switch (para.SegmentType)
                {
                    case SegmentTypes.None:
                        await HandleParamGhost(para, types[para.ParameterTypeId], adds);
                        break;

                    case SegmentTypes.Memory:
                        para.Value = await GetValueFromMem(para, paraT);
                        break;

                    case SegmentTypes.Property:
                        //TODO implement also Properties
                        break;
                }
            }

            _data.Parameters = paras;
            SaveConfig(adds);
        }

        private void SaveConfig(AppAdditional adds)
        {
            TodoText = "Speichere Konfiguration...";

            Dictionary<string, ViewParamModel> Id2Param = new Dictionary<string, ViewParamModel>();
            Dictionary<string, ChangeParamModel> ParaChanges = new Dictionary<string, ChangeParamModel>();
            Dictionary<string, ViewParamModel> VisibleParams = new Dictionary<string, ViewParamModel>();
            List<IDynChannel> Channels = SaveHelper.ByteArrayToObject<List<IDynChannel>>(adds.ParamsHelper, true);
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

            Dictionary<string, List<List<ParamCondition>>> para2Conds = new Dictionary<string, List<List<ParamCondition>>>();
            foreach (IDynChannel ch in Channels)
            {
                foreach (ParameterBlock block in ch.Blocks)
                {
                    foreach (IDynParameter para in block.Parameters)
                    {
                        if (_data.Parameters.ContainsKey(para.Id))
                            para.Value = _data.Parameters[para.Id].Value;

                        if (!Id2Param.ContainsKey(para.Id))
                            Id2Param.Add(para.Id, new ViewParamModel(para.Value));

                        Id2Param[para.Id].Parameters.Add(para);

                        if (!para2Conds.ContainsKey(para.Id))
                            para2Conds.Add(para.Id, new List<List<ParamCondition>>());

                        para2Conds[para.Id].Add(para.Conditions);
                    }
                }
            }

            foreach (IDynChannel ch in Channels)
            {
                if (SaveHelper.CheckConditions(ch.Conditions, Id2Param))
                {
                    foreach (ParameterBlock block in ch.Blocks)
                    {
                        if(SaveHelper.CheckConditions(block.Conditions, Id2Param))
                        {
                            foreach (IDynParameter para in block.Parameters)
                            {
                                if (!VisibleParams.ContainsKey(para.Id))
                                    VisibleParams.Add(para.Id, Id2Param[para.Id]);
                            }
                        }
                    }
                }
            }



            foreach (AppParameter newPara in _data.Parameters.Values)
            {
                if (newPara.Access != AccessType.Full || !VisibleParams.ContainsKey(newPara.Id)) continue;

                bool isOneVisible = false;
                foreach (IDynParameter dPara in VisibleParams[newPara.Id].Parameters)
                {
                    if (SaveHelper.CheckConditions(dPara.Conditions, Id2Param))
                        isOneVisible = true;
                }

                if (!isOneVisible) continue;


                string oldPara = defParas[newPara.Id];
                bool changeExists = ParaChanges.ContainsKey(newPara.Id);
                ChangeParamModel change = null;
                if (changeExists)
                    change = ParaChanges[newPara.Id];

                if ((changeExists && newPara.Value != change.Value) || (!changeExists && newPara.Value != oldPara))
                {
                    ChangeParamModel ch = new ChangeParamModel();
                    ch.DeviceId = Device.UId;
                    ch.ParamId = newPara.Id;
                    ch.Value = newPara.Value;
                    _= App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        ChangeHandler.Instance.ChangedParam(ch);
                    });
                }
            }

            TodoText = "Generiere KOs";

            GenerateComs(Id2Param);

            Finish();
        }

        private async Task HandleParamGhost(AppParameter para, AppParameterTypeViewModel paraT, AppAdditional adds)
        {
            XDocument dynamic = XDocument.Parse(Encoding.UTF8.GetString(adds.Dynamic));

            string ns = dynamic.Root.Name.NamespaceName;
            IEnumerable<XElement> chooses = dynamic.Descendants(XName.Get("choose", ns)).Where(c => c.Attribute("ParamRefId") != null && SaveHelper.ShortId(c.Attribute("ParamRefId").Value) == para.Id);

            foreach(XElement choose in chooses)
            {
                Dictionary<string, List<int>> value2Coms = new Dictionary<string, List<int>>();

                #region Get ComIds and remove duplicates
                foreach (XElement when in choose.Elements())
                {
                    if (when.Attribute("default")?.Value == "true") continue;
                    string val = when.Attribute("test").Value;

                    if (val.Contains(">") || val.Contains("<") || val.Contains("=") || val.Contains(" ")) continue;
                    if (!value2Coms.ContainsKey(val)) value2Coms[val] = new List<int>();

                    IEnumerable<XElement> tlist = when.Descendants(XName.Get("ComObjectRefRef", ns));
                    foreach (XElement comx in tlist)
                    {
                        string id = SaveHelper.ShortId(comx.Attribute("RefId").Value);
                        AppComObject com = _context.AppComObjects.Single(c => c.Id == id);

                        if (!value2Coms[val].Contains(com.Number))
                            value2Coms[val].Add(com.Number);
                    }
                }

                List<int> toDelete = new List<int>();
                foreach(string keyval in value2Coms.Keys)
                {
                    List<int> xids = value2Coms[keyval];


                    foreach (int xid in xids)
                    {
                        bool flag = false;

                        foreach (KeyValuePair<string, List<int>> otherids in value2Coms.Where(x => x.Key != keyval))
                            if (otherids.Value.Contains(xid))
                                flag = true;

                        if (flag) toDelete.Add(xid);
                    }
                }


                foreach(int xid in toDelete)
                {
                    foreach (KeyValuePair<string, List<int>> otherids in value2Coms)
                        otherids.Value.Remove(xid);
                }
                #endregion


                //Hier weiter
                Dictionary<string, bool> val2success = new Dictionary<string, bool>();

                foreach(KeyValuePair<string, List<int>> coms in value2Coms)
                {
                    val2success[coms.Key] = false;

                    foreach(int comNr in coms.Value)
                    {
                        if (connectedCOs.Contains(comNr))
                        {
                            val2success[coms.Key] = true;
                        }
                    }
                }


                if(val2success.Count(y => y.Value == true) == 1)
                {
                    KeyValuePair<string, bool> success = val2success.Single(y => y.Value == true);
                    para.Value = success.Key;
                }
                else
                {
                    await HandleParamGhost2(para, paraT, adds, choose);
                }
                
            }
        }

        private async Task HandleParamGhost2(AppParameter para, AppParameterTypeViewModel paraT, AppAdditional adds, XElement choose)
        {
            string ns = choose.Name.NamespaceName;
            Dictionary<string, List<string>> value2Paras = new Dictionary<string, List<string>>();
            Dictionary<string, string> test = new Dictionary<string, string>();

            #region Get ParaIds and remove duplicates
            foreach (XElement when in choose.Elements())
            {
                if (when.Attribute("default")?.Value == "true" || when.Attribute("default")?.Value == para.Value) continue;
                string val = when.Attribute("test").Value;

                if (val.Contains(">") || val.Contains("<") || val.Contains("=") || val.Contains(" ")) continue;
                if (!value2Paras.ContainsKey(val)) value2Paras[val] = new List<string>();

                IEnumerable<XElement> tlist = when.Descendants(XName.Get("ParameterRefRef", ns));
                foreach (XElement comx in tlist)
                {
                    string id = SaveHelper.ShortId(comx.Attribute("RefId").Value);
                    AppParameter par = _context.AppParameters.Single(c => c.Id == id);
                    if (par.Access != AccessType.Full || par.SegmentId == null) continue;



                    string oldVal = defParas[par.Id];

                    AppParameterTypeViewModel partype = _context.AppParameterTypes.Single(pt => pt.Id == par.ParameterTypeId);
                    string newVal = await GetValueFromMem(par, partype);

                    if (oldVal != newVal && !value2Paras[val].Contains(id))
                    {
                        test.Add(par.Id, newVal);
                        value2Paras[val].Add(id);
                    }
                }
            }

            List<string> toDelete = new List<string>();
            foreach (string keyval in value2Paras.Keys)
            {
                List<string> xids = value2Paras[keyval];


                foreach (string xid in xids)
                {
                    bool flag = false;

                    foreach (KeyValuePair<string, List<string>> otherids in value2Paras.Where(x => x.Key != keyval))
                        if (otherids.Value.Contains(xid))
                            flag = true;

                    if (flag) toDelete.Add(xid);
                }
            }


            foreach (string xid in toDelete)
            {
                foreach (KeyValuePair<string, List<string>> otherids in value2Paras)
                    otherids.Value.Remove(xid);
            }
            #endregion


            Dictionary<string, bool> val2success = new Dictionary<string, bool>();

            foreach (KeyValuePair<string, List<string>> coms in value2Paras)
            {
                val2success[coms.Key] = coms.Value.Count > 0;
            }


            if (val2success.Count(y => y.Value == true) == 1)
            {
                KeyValuePair<string, bool> success = val2success.Single(y => y.Value == true);
                para.Value = success.Key;
            }
        }

        private async Task<string> GetValueFromMem(AppParameter para, AppParameterTypeViewModel paraT)
        {
            if (para.SegmentId == null) return "";
            if (!mems.ContainsKey(para.SegmentId))
            {
                AppSegmentViewModel seg = _context.AppSegments.Single(s => s.Id == para.SegmentId);
                byte[] temp = await dev.MemoryRead(seg.Address, seg.Size);
                mems.Add(para.SegmentId, temp);
            }


            byte[] bdata = null;
            int sizeB;

            if (paraT.Size % 8 == 0)
            {
                sizeB = paraT.Size / 8;
                bdata = new byte[sizeB];
                for (int i = 0; i < sizeB; i++)
                {
                    bdata[i] = mems[para.SegmentId][para.Offset + i];
                }
            }
            else
            {
                sizeB = 1;
                bdata = new byte[sizeB];
                byte temp = mems[para.SegmentId][para.Offset];

                var y = new BitArray(new byte[] { temp });
                var z = new BitArray(8);

                for(int i = 0; i < paraT.Size; i++)
                {
                    z.Set(i, y.Get(i + para.OffsetBit));
                }
                z.CopyTo(bdata, 0);
            }


            switch (paraT.Type)
            {
                case ParamTypes.Enum:
                case ParamTypes.NumberInt:
                case ParamTypes.NumberUInt:
                    int x = 0;
                    switch (bdata.Length)
                    {
                        case 1:
                            x = BitConverter.ToInt16(new byte[2] { bdata[0], 0 }, 0);
                            break;

                        case 2:
                            Array.Reverse(bdata);
                            x = BitConverter.ToInt16(bdata, 0);
                            break;

                        case 3:
                            Array.Reverse(bdata);
                            x = BitConverter.ToInt32(new byte[4] { bdata[0], bdata[1], bdata[2], 0 }, 0);
                            break;

                        case 4:
                            Array.Reverse(bdata);
                            x = BitConverter.ToInt32(bdata, 0);
                            break;
                    }

                    return x.ToString();

                case ParamTypes.Text:
                    return Encoding.UTF8.GetString(bdata);
            }
            return "";
        }


        private async void GenerateComs(Dictionary<string, ViewParamModel> Id2Param)
        {
            AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == Device.ApplicationId);
            List<DeviceComObject> comObjects = SaveHelper.ByteArrayToObject<List<DeviceComObject>>(adds.ComsAll);
            List<ParamBinding> Bindings = SaveHelper.ByteArrayToObject<List<ParamBinding>>(adds.Bindings);
            ProjectContext _contextP = new ProjectContext(SaveHelper.connProject);

            List<DeviceComObject> newObjs = new List<DeviceComObject>();
            foreach (DeviceComObject obj in comObjects)
            {
                if (obj.Conditions.Count == 0)
                {
                    newObjs.Add(obj);
                    continue;
                }

                bool flag = SaveHelper.CheckConditions(obj.Conditions, Id2Param);
                if (flag)
                    newObjs.Add(obj);
            }


            List<DeviceComObject> toAdd = new List<DeviceComObject>();
            foreach (DeviceComObject cobj in newObjs)
            {
                if (!Device.ComObjects.Any(co => co.Id == cobj.Id))
                    toAdd.Add(cobj);
            }

            Dictionary<string, ComObject> coms = new Dictionary<string, ComObject>();
            foreach (ComObject com in _contextP.ComObjects)
                if (!coms.ContainsKey(com.ComId))
                    coms.Add(com.ComId, com);

            Dictionary<string, FunctionGroup> groupsMap = new Dictionary<string, FunctionGroup>();

            foreach (Building building in SaveHelper._project.Area.Buildings)
                foreach (Floor floor in building.Floors)
                    foreach (Room room in floor.Rooms)
                        foreach (Function func in room.Functions)
                            foreach (FunctionGroup funcG in func.Subs)
                                if (!groupsMap.ContainsKey(funcG.Address.ToString()))
                                    groupsMap.Add(funcG.Address.ToString(), funcG);

            bool flagGroups = false;

            foreach (DeviceComObject dcom in toAdd)
            {
                List<string> groupIds = new List<string>();

                if (CO2GA.ContainsKey(dcom.Number))
                    groupIds = CO2GA[dcom.Number];

                if (dcom.Name.Contains("{{"))
                {
                    ParamBinding bind = Bindings.Single(b => b.Hash == "CO:" + dcom.Id);
                    string value = Id2Param[dcom.BindedId].Value;
                    if (string.IsNullOrEmpty(value))
                        dcom.DisplayName = dcom.Name.Replace("{{dyn}}", bind.DefaultText);
                    else
                        dcom.DisplayName = dcom.Name.Replace("{{dyn}}", value);
                }
                else
                {
                    dcom.DisplayName = dcom.Name;
                }

                await App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Device.ComObjects.Add(dcom);
                });

                ComObject com = new ComObject();
                com.ComId = dcom.Id;
                com.DeviceId = Device.UId;

                if (groupIds.Count > 0) com.Groups = string.Join(",", groupIds);
                foreach(string groupId in groupIds)
                {
                    if (groupsMap.ContainsKey(groupId))
                    {
                        dcom.Groups.Add(groupsMap[groupId]);
                        groupsMap[groupId].ComObjects.Add(dcom);
                    } else
                    {
                        flagGroups = true;
                    }
                }

                _contextP.ComObjects.Add(com);
            }


            await App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Device.ComObjects.Sort(s => s.Number);

                if (flagGroups)
                    ViewHelper.Instance.ShowNotification("main", "Es konnten einige Gruppenadressen nicht zugeordnet werden, da diese nicht im Projekt existieren.", 4000, ViewHelper.MessageType.Warning);
            });
            _contextP.SaveChanges();
        }


        private void Finish(string errmsg = null)
        {
            ProgressValue = 100;

            if (string.IsNullOrEmpty(errmsg))
            {
                TodoText = "Erfolgreich";

                Device.LoadedPA = true;
                Device.LoadedGroup = true;
                Device.LoadedApplication = true;
                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => SaveHelper.UpdateDevice(Device));

                Finished?.Invoke(this, _data);
            }
            else
            {
                TodoText = "Fehlgeschlagen";
                Finished?.Invoke(this, new ErrorData(_data, errmsg));
            }

            Analytics.TrackEvent("Geräte Konfig auslesen");
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
    }
}
