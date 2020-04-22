using Kaenx.Classes.Bus.Data;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Classes;
using Microsoft.AppCenter.Analytics;
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
using static Kaenx.Classes.Bus.Actions.IBusAction;

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

        public string Type { get; } = "Geräte Speicher";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public Connection Connection { get; set; }

        public event ActionFinishedHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;

        public DeviceConfig()
        { 
        }

        public void Run(CancellationToken token)
        {
            _token = token; // TODO implement cancellation
            TodoText = "Lese Gerätespeicher...";

            Start();
        }

        private async void Start()
        {
            dev = new BusDevice(Device.LineName, Connection);
            dev.Connect();

            await Task.Delay(100);

            #region Grundinfo
            TodoText = "Lese Maskenversion...";
            dev.Connect();

            await Task.Delay(100);

            _data.MaskVersion = "MV-" + await dev.DeviceDescriptorRead();

            ProgressValue = 10;
            TodoText = "Lese Seriennummer...";
            await Task.Delay(500);
            try
            {
                _data.SerialNumber = await dev.PropertyRead<string>(_data.MaskVersion, "DeviceSerialNumber");
            }
            catch (Exception e)
            {
                _data.SerialNumber = e.Message;
            }


            ProgressValue = 20;
            TodoText = "Lese Applikations Id...";
            await Task.Delay(500);
            string appId = await dev.PropertyRead<string>(_data.MaskVersion, "ApplicationId");
            if (appId.Length == 8) appId = "00" + appId;
            appId = "M-" + appId.Substring(0, 4) + "_A-" + appId.Substring(4, 4) + "-" + appId.Substring(8, 2) + "-";

            CatalogContext context = new CatalogContext();

            XElement master = (await GetKnxMaster()).Root;
            XElement manu = master.Descendants(XName.Get("Manufacturer", master.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == appId.Substring(0, 6));
            _data.Manufacturer = manu.Attribute("Name").Value;
            Hardware2AppModel h2a = null;

            try
            {
                h2a = context.Hardware2App.First(h => h.ApplicationId.StartsWith(appId));
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

            #endregion

            ProgressValue = 30;
            TodoText = "Lese Assoziationstabelle...";
            await Task.Delay(500);

            ApplicationViewModel appModel = null;

            if (context.Applications.Any(a => a.Id.StartsWith(appId)))
            {
                appModel = context.Applications.Single(a => a.Id.StartsWith(appId)); //TODO check if now complete appid is returned
            }

            if(appModel != null)
            {
                if (appModel.Table_Assosiations != "" || appModel.Table_Assosiations != null)
                {
                    AppSegmentViewModel segmentModel = context.AppAbsoluteSegments.Single(s => s.Id == appModel.Table_Assosiations);
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
                            int COnr = datax[offset + 1];

                            if (!connectedCOs.Contains(COnr))
                                connectedCOs.Add(COnr);
                        }
                    }

                }
            }


            ProgressValue = 40;
            TodoText = "Berechne Kofiguration...";

            GetConfig(h2a);
        }




        private async void GetConfig(Hardware2AppModel h2a)
        {
            Dictionary<string, AppParameter> paras = new Dictionary<string, AppParameter>();

            foreach (AppParameter param in _context.AppParameters.Where(p => p.ApplicationId == h2a.ApplicationId))
                paras.Add(param.Id, param);

            AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == h2a.ApplicationId);


            foreach(AppParameter para in paras.Values)
            {
                AppParameterTypeViewModel paraT = _context.AppParameterTypes.Single(t => t.Id == para.ParameterTypeId);
                if (paraT.Size == 0) continue;

                switch (para.SegmentType)
                {
                    case SegmentTypes.None:
                        await HandleParamGhost(para, paraT, adds);
                        break;

                    case SegmentTypes.Memory:
                        await HandleParamMem(para, paraT);
                        break;

                    case SegmentTypes.Property:

                        break;
                }
            }

            _data.Parameters = paras;
            Finish();
        }


        private async Task HandleParamGhost(AppParameter para, AppParameterTypeViewModel paraT, AppAdditional adds)
        {
            XDocument dynamic = XDocument.Parse(Encoding.UTF8.GetString(adds.Dynamic));

            string ns = dynamic.Root.Name.NamespaceName;
            IEnumerable<XElement> chooses = dynamic.Descendants(XName.Get("choose", ns)).Where(c => c.Attribute("ParamRefId")?.Value == para.Id);

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
                        string id = comx.Attribute("RefId").Value;
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
                    para.Access = AccessType.Read;
                }
                
            }
        }


        private async Task HandleParamMem(AppParameter para, AppParameterTypeViewModel paraT)
        {
            if (para.SegmentId == null) return;

            if(!mems.ContainsKey(para.SegmentId))
            {
                AppSegmentViewModel seg = _context.AppAbsoluteSegments.Single(s => s.Id == para.SegmentId);
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
                Dictionary<int, int> sizeMap = new Dictionary<int, int>() {
                            { 8, 0b11111111 },
                            { 7, 0b01111111 },
                            { 6, 0b00111111 },
                            { 5, 0b00011111 },
                            { 4, 0b00001111 },
                            { 3, 0b00000111 },
                            { 2, 0b00000011 },
                            { 1, 0b00000001 },
                        };

                int x = temp >> (8 - (para.OffsetBit + paraT.Size));
                int mask = sizeMap[paraT.Size];
                x = x & mask;
                bdata[0] = Convert.ToByte(x);
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

                    para.Value = x.ToString();
                    break;

                case ParamTypes.Text:
                    para.Value = Encoding.UTF8.GetString(bdata);
                    break;
            }

            if (para.Access == AccessType.Full)
                para.Access = AccessType.Read;
        }






        private void Finish(string errmsg = null)
        {
            ProgressValue = 100;

            if (string.IsNullOrEmpty(errmsg))
            {
                TodoText = "Erfolgreich";
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
