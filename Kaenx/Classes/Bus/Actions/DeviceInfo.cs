using Kaenx.Classes.Bus.Data;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Connections;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.UI.Xaml.Data;
using static Kaenx.Classes.Bus.Actions.IBusAction;
using static Kaenx.Classes.Bus.Data.DeviceInfoData;

namespace Kaenx.Classes.Bus.Actions
{
    public class DeviceInfo : IBusAction, INotifyPropertyChanged
    {
        private int _progress;
        private bool _progressIsIndeterminate;
        private string _todoText;
        private DeviceInfoData _data = new DeviceInfoData();
        private CancellationToken _token;

        public string Type { get; } = "Geräte Info";
        public LineDevice Device { get; set; }
        public int ProgressValue { get { return _progress; } set { _progress = value; Changed("ProgressValue"); } }
        public bool ProgressIsIndeterminate { get { return _progressIsIndeterminate; } set { _progressIsIndeterminate = value; Changed("ProgressIsIndeterminate"); } }
        public string TodoText { get => _todoText; set { _todoText = value; Changed("TodoText"); } }

        public IKnxConnection Connection { get; set; }

        public event ActionFinishedHandler Finished;
        public event PropertyChangedEventHandler PropertyChanged;

        public DeviceInfo()
        { 
        }

        public void Run(CancellationToken token)
        {
            _token = token; // TODO implement cancellation
            TodoText = "Lese Geräteinfo...";

            Start();
        }

        private async void Start()
        {
            try
            {
                BusDevice dev = new BusDevice(Device.LineName, Connection);

                TodoText = "Lese Maskenversion...";
                await dev.Connect();

                //_data.Description = await dev.PropertyRead<string>(0, 21);
                _data.SupportsEF = dev.SupportsExtendedFrames;
                _data.MaskVersion = await dev.DeviceDescriptorRead();



                XElement master = (await GetKnxMaster()).Root;
                XElement maskEle = master.Descendants(XName.Get("MaskVersion", master.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == _data.MaskVersion);

                List<string> dontread = new List<string>() { "ManagementStyle", "DeviceBusVoltage", "GroupAddressTable", "GroupAssociationTable", "GroupObjectTable" };

                IEnumerable<XElement> resources = maskEle.Descendants(XName.Get("Resource", master.Name.NamespaceName)).Where(res =>
                {
                    string name = res.Attribute("Name").Value;
                    XElement access = res.Element(XName.Get("AccessRights", master.Name.NamespaceName));
                    if (access.Attribute("Read").Value == "None") return false;
                    return !dontread.Contains(name) && !name.EndsWith("Ptr");
                });
                //TODO auch InterfaceObject Property auslesen

                int stepsize = (int)(100 / (resources.Count() + 4));

                ProgressValue += stepsize;
                TodoText = "Lese Seriennummer...";
                //await Task.Delay(500);

                try
                {
                    _data.SerialNumber = await dev.PropertyRead<string>(0,11);
                }
                catch (Exception e)
                {
                    _data.SerialNumber = e.Message;
                }


                ProgressValue += stepsize;
                TodoText = "Lese Applikations Id...";
                //await Task.Delay(500);
                string appId = await dev.RessourceRead<string>("ApplicationId");
                if (appId.Length == 8) appId = "00" + appId;
                appId = "M-" + appId.Substring(0, 4) + "_A-" + appId.Substring(4, 4) + "-" + appId.Substring(8, 2);

                CatalogContext context = new CatalogContext();

                XElement manu = master.Descendants(XName.Get("Manufacturer", master.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == appId.Substring(0, 6));
                _data.Manufacturer = manu.Attribute("Name").Value;

                try
                {
                    Hardware2AppModel h2a = context.Hardware2App.First(h => h.ApplicationId == appId);
                    DeviceViewModel dvm = context.Devices.First(d => d.HardwareId == h2a.HardwareId);
                    _data.DeviceName = dvm.Name;
                    _data.ApplicationName = h2a.Name + " " + h2a.VersionString;
                }
                catch
                {
                    _data.DeviceName = "Unbekannt";
                    _data.ApplicationName = "Applikation nicht im Katalog";
                }

                if (Device != null && !Device.ApplicationId?.StartsWith(appId) == true)
                {
                    _data.Additional = "Warnung! Applikations Id im Gerät stimmt nicht mit dem im Projekt überein!";
                }

                _data.ApplicationId = appId;


                ProgressValue += stepsize;
                TodoText = "Lese Gruppentabelle...";
                //await Task.Delay(500);
                int grpAddr = -1;

                ApplicationViewModel appModel = null;

                List<byte[]> datas = new List<byte[]>();

                if (context.Applications.Any(a => a.Id == appId))
                {
                    appModel = context.Applications.Single(a => a.Id == appId); //TODO check if now complete appid is returned

                    if (appModel.IsRelativeSegment)
                    {
                        Finish("Geräte mit Relativem Segment werden noch nicht unterstützt...");
                        return;
                    }

                    if (!string.IsNullOrEmpty(appModel.Table_Group))
                    {
                        AppSegmentViewModel segmentModel = context.AppSegments.Single(s => s.Id == appModel.Table_Group);
                        grpAddr = segmentModel.Address;
                    }
                }

                if (grpAddr == -1)
                {
                    grpAddr = await dev.PropertyRead<int>(1, 7);
                }


                if (grpAddr != -1)
                {
                    byte[] datax = await dev.MemoryRead(grpAddr, 1);

                    if (datax.Length > 0)
                    {
                        int length = Convert.ToInt16(datax[0]) - 1;
                        datax = await dev.MemoryRead(grpAddr + 3, length * 2);
                        datas.Add(datax);

                        List<MulticastAddress> addresses = new List<MulticastAddress>();
                        for (int i = 0; i < (datax.Length / 2); i++)
                        {
                            int offset = i * 2;
                            addresses.Add(MulticastAddress.FromByteArray(new byte[] { datax[offset], datax[offset + 1] }));
                        }

                        _data.GroupTable = addresses;
                    }
                }


                ProgressValue += stepsize;
                TodoText = "Lese Assoziationstabelle...";
                //await Task.Delay(500);

                if (appModel != null)
                {
                    int assoAddr = -1;

                    if (!string.IsNullOrEmpty(appModel.Table_Assosiations))
                    {
                        AppSegmentViewModel segmentModel = context.AppSegments.Single(s => s.Id == appModel.Table_Assosiations);
                        assoAddr = segmentModel.Address;
                    }

                    if(assoAddr == -1)
                    {
                        assoAddr = await dev.PropertyRead<int>(2, 7);
                    }


                    if(assoAddr != -1)
                    {
                        byte[] datax = await dev.MemoryRead(assoAddr, 1);
                        if (datax.Length > 0)
                        {
                            int length = Convert.ToInt16(datax[0]);

                            datax = await dev.MemoryRead(assoAddr + 1, length * 2);
                            datas.Add(datax);

                            List<AssociationHelper> table = new List<AssociationHelper>();
                            for (int i = 0; i < length; i++)
                            {
                                int offset = i * 2;

                                AssociationHelper helper = new AssociationHelper()
                                {
                                    ObjectIndex = datax[offset + 1]
                                };

                                if (_data.GroupTable.Count > 0)
                                    helper.GroupIndex = _data.GroupTable[datax[offset] - 1].ToString();
                                else
                                    helper.GroupIndex = datax[offset].ToString();

                                if (Device.ComObjects.Any(c => c.Number == helper.ObjectIndex))
                                {
                                    Project.DeviceComObject com = Device.ComObjects.Single(c => c.Number == helper.ObjectIndex);
                                    helper.ObjectInfo = com.DisplayName;
                                    helper.ObjectFunc = com.Function;
                                }


                                table.Add(helper);
                            }

                            _data.AssociationTable = table;
                        }
                    }

                }


                Dictionary<string, GroupInfoCollection<OtherResource>> dic = new Dictionary<string, GroupInfoCollection<OtherResource>>();

                TodoText = "Lese andere Resourcen...";
                foreach (XElement resource in resources)
                {
                    byte[] value = await dev.RessourceRead(resource.Attribute("Name").Value);

                    OtherResource resx = new OtherResource();
                    resx.Name = resource.Attribute("Name").Value;
                    resx.Value = BitConverter.ToString(value != null ? value : new byte[] { 0x00 });
                    resx.ValueRaw = BitConverter.ToString(value != null ? value : new byte[] { 0x00 });

                    if(resx.Name.StartsWith("Pei"))
                    {
                        if (!dic.ContainsKey("Applikation 2"))
                            dic.Add("Applikation 2", new GroupInfoCollection<OtherResource>() { Key = "Applikation 2" });
                    } else if (resx.Name.StartsWith("Application"))
                    {
                        if (!dic.ContainsKey("Applikation"))
                            dic.Add("Applikation", new GroupInfoCollection<OtherResource>() { Key = "Applikation" });
                    }
                    else if (resx.Name.StartsWith("Group"))
                    {
                        if (!dic.ContainsKey("Group"))
                            dic.Add("Group", new GroupInfoCollection<OtherResource>() { Key = "Group" });
                    }
                    else
                    {
                        if (!dic.ContainsKey("Allgemein"))
                            dic.Add("Allgemein", new GroupInfoCollection<OtherResource>() { Key = "Allgemein" });
                    }

                    //TODO übersetzung der Property anzeigen

                    if (resx.Name.StartsWith("Pei"))
                    {
                        dic["Applikation 2"].Add(resx);
                    }
                    else if (resx.Name.StartsWith("Application"))
                    {
                        dic["Applikation"].Add(resx);
                    }
                    else if (resx.Name.StartsWith("Group"))
                    {
                        dic["Group"].Add(resx);
                    }
                    else
                    {
                        dic["Allgemein"].Add(resx);
                    }
                    ProgressValue += stepsize;
                }
                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    _data.OtherResources = (new CollectionViewSource() { IsSourceGrouped = true, Source = new ObservableCollection<GroupInfoCollection<OtherResource>>(dic.Values) }).View;
                });
            } catch(OperationCanceledException)
            {
                Finish("Gerät antwortet nicht");
                return;
            }catch(Exception e)
            {
                Finish(e.Message +  Environment.NewLine + e.StackTrace);
                return;
            }

            if(!string.IsNullOrEmpty(Device.ApplicationId))
            {
                Device.LoadedPA = true;
                _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => SaveHelper.UpdateDevice(Device));
            }


            Finish();
        }


        private void Finish(string text = "")
        {
            ProgressValue = 100;
            Analytics.TrackEvent("Geräte Info auslesen");

            if (string.IsNullOrEmpty(text))
            {
                TodoText = "Erfolgreich";
                Finished?.Invoke(this, _data);
            }
            else
            {
                TodoText = text;
                Finished?.Invoke(this, new ErrorData(_data, text));
            }

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
