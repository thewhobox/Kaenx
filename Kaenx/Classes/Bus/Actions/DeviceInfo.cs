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

                IEnumerable<XElement> xresources = maskEle.Descendants(XName.Get("Resource", master.Name.NamespaceName)).Where(res =>
                {
                    string name = res.Attribute("Name").Value;
                    XElement access = res.Element(XName.Get("AccessRights", master.Name.NamespaceName));
                    if (access.Attribute("Read").Value == "None") return false;
                    return !dontread.Contains(name) && !name.EndsWith("Ptr");
                });
                //TODO auch InterfaceObject Property auslesen

                int stepsize = (int)(100 / (xresources.Count() + 4));

                ProgressValue += stepsize;
                TodoText = "Lese Seriennummer...";
                //await Task.Delay(500);

                try
                {
                    _data.SerialNumber = await dev.PropertyRead<string>(0, 11);
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

                XElement xmanu = master.Descendants(XName.Get("Manufacturer", master.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == appId.Substring(0, 6));
                _data.Manufacturer = xmanu.Attribute("Name").Value;

                try
                {
                    ApplicationViewModel app = null;

                    try
                    {
                        int manu = int.Parse(appId.Substring(0, 4), System.Globalization.NumberStyles.HexNumber);
                        int number = int.Parse(appId.Substring(4, 4), System.Globalization.NumberStyles.HexNumber);
                        int version = int.Parse(appId.Substring(8, 2), System.Globalization.NumberStyles.HexNumber);
                        app = context.Applications.Single(a => a.Manufacturer == manu && a.Number == number && a.Version == version);
                    }
                    catch { }

                    if(app != null)
                    {
                        Hardware2AppModel h2a = context.Hardware2App.First(h => h.Id == app.HardwareId);
                        DeviceViewModel dvm = context.Devices.First(d => d.HardwareId == h2a.Id);
                        _data.DeviceName = dvm.Name;
                        _data.ApplicationName = h2a.Name + " " + h2a.VersionString;
                    }

                    
                }
                catch
                {
                    _data.DeviceName = "Unbekannt";
                    _data.ApplicationName = "Applikation nicht im Katalog";
                }

                //TODO check change
                if (Device != null && true) // !Device.ApplicationId?.StartsWith(appId) == true)
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

                //TODO check change
                if (context.Applications.Any(a => true)) //a.Id == appId))
                {
                    appModel = context.Applications.Single(a => true); // a.Id == appId); //TODO check if now complete appid is returned

                    if (appModel.IsRelativeSegment)
                    {
                        Finish("Geräte mit Relativem Segment werden noch nicht unterstützt...");
                        return;
                    }

                    if (appModel.Table_Group != -1)
                    {
                        //TODO check changed
                        //AppSegmentViewModel segmentModel = context.AppSegments.Single(s => s.Id == appModel.Table_Group);
                        //grpAddr = segmentModel.Address + appModel.Table_Group_Offset;
                    }
                }

                if (grpAddr == -1)
                {
                    grpAddr = await dev.RessourceAddress("GroupAddressTable");
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

                int assoAddr = -1;
                if (appModel != null)
                {
                    if (appModel.Table_Assosiations != null)
                    {
                        //TODO check change
                        //AppSegmentViewModel segmentModel = context.AppSegments.Single(s => s.ApplicationId == appModel.Table_Assosiations);
                        //assoAddr = segmentModel.Address + appModel.Table_Assosiations_Offset;
                    }
                }

                if (assoAddr == -1)
                {
                    assoAddr = await dev.RessourceAddress("GroupAssociationTable");
                    //assoAddr = await dev.PropertyRead<int>(2, 7);
                }


                if (assoAddr != -1)
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


                //Dictionary<string, GroupInfoCollection<OtherResource>> dic = new Dictionary<string, GroupInfoCollection<OtherResource>>();
                _data.OtherResources = new List<OtherResource>();

                TodoText = "Lese andere Resourcen...";
                foreach (XElement resource in xresources)
                {
                    byte[] value = await dev.RessourceRead(resource.Attribute("Name").Value);

                    OtherResource resx = new OtherResource();
                    resx.Name = resource.Attribute("Name").Value;
                    resx.ValueRaw = BitConverter.ToString(value != null ? value : new byte[] { 0x00 });



                    switch (resource.Attribute("Name").Value)
                    {
                        case "DeviceManufacturerId":
                            string dmanu = BitConverter.ToString(value).Replace("-", "");
                            for (int i = dmanu.Length; i < 4; i++) {
                                dmanu = "0" + dmanu;
                            }
                            XElement xmanu2 = master.Descendants(XName.Get("Manufacturer", master.Name.NamespaceName)).Single(m => m.Attribute("Id")?.Value == "M-" + dmanu);
                            resx.Value = xmanu2.Attribute("Name").Value;
                            break;

                        case "IndividualAddress":
                            resx.Value = UnicastAddress.FromByteArray(value).ToString();
                            break;

                        default:
                            resx.Value = BitConverter.ToString(value != null ? value : new byte[] { 0x00 });
                            break;
                    }


                    _data.OtherResources.Add(resx);

                    /*if(resx.Name.StartsWith("Pei"))
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
                    }*/
                    ProgressValue += stepsize;
                }
            } catch (OperationCanceledException)
            {
                Finish("Gerät antwortet nicht");
                return;
            }catch(TimeoutException te) {

                Finish("Gerät antwortet nicht in angemessener Zeit");
                _data.Additional = "Gerät antwortet nicht in angemessener Zeit";
                return;
            }
            catch(Exception e)
            {
                Finish(e.Message +  Environment.NewLine + e.StackTrace);
                return;
            }

            if(Device.ApplicationId >= 0)
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
