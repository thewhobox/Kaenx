using Kaenx.Classes.Bus.Data;
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

        public Connection Connection { get; set; }

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
            BusDevice dev = new BusDevice(Device.LineName, Connection);
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
            } catch(Exception e)
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

            try
            {
                Hardware2AppModel h2a = context.Hardware2App.First(h => h.ApplicationId.StartsWith(appId));
                DeviceViewModel dvm = context.Devices.First(d => d.HardwareId == h2a.HardwareId);
                _data.DeviceName = dvm.Name;
                _data.ApplicationName = h2a.Name + " " + h2a.VersionString;
            }
            catch {
                _data.DeviceName = "Unbekannt";
                _data.ApplicationName = "Applikation nicht im Katalog";
            }

            if(Device != null && !Device.ApplicationId?.StartsWith(appId) == true)
            {
                _data.Additional = "Warnung! Applikations Id im Gerät stimmt nicht mit dem im Projekt überein!";
            }

            _data.ApplicationId = appId + "XXXX";


            ProgressValue = 30;
            TodoText = "Lese Gruppentabelle...";
            await Task.Delay(500);
            int grpAddr = -1;

            ApplicationViewModel appModel = null;

            if (context.Applications.Any(a => a.Id.StartsWith(appId)))
            {
                appModel = context.Applications.Single(a => a.Id.StartsWith(appId)); //TODO check if now complete appid is returned

                if (appModel.Table_Group != "" || appModel.Table_Group != null)
                {
                    AppSegmentViewModel segmentModel = context.AppAbsoluteSegments.Single(s => s.Id == appModel.Table_Group);
                    grpAddr = segmentModel.Address;
                }
            }

            if(grpAddr == -1)
            {
                try
                {
                    grpAddr = await dev.PropertyRead<int>(_data.MaskVersion, "GroupAddressTable");
                }
                catch { 
                }
            }


            if(grpAddr != -1)
            {
                byte[] datax = await dev.MemoryRead(grpAddr, 1);

                if(datax.Length > 0)
                {
                    int length = Convert.ToInt16(datax[0]) - 1;
                    datax = await dev.MemoryRead(grpAddr + 3, length * 2);

                    List<MulticastAddress> addresses = new List<MulticastAddress>();
                    for (int i = 0; i < (datax.Length / 2); i++)
                    {
                        int offset = i * 2;
                        addresses.Add(MulticastAddress.FromByteArray(new byte[] { datax[offset], datax[offset + 1] }));
                    }

                    _data.GroupTable = addresses;
                }
            }


            ProgressValue = 30;
            TodoText = "Lese Assoziationstabelle...";
            await Task.Delay(500);

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

                            AssociationHelper helper = new AssociationHelper()
                            {
                                ObjectIndex = datax[offset + 1]
                            };

                            if (_data.GroupTable.Count > 0)
                                helper.GroupIndex = _data.GroupTable[datax[offset] - 1].ToString();
                            else
                                helper.GroupIndex = datax[offset].ToString();

                            if(Device.ComObjects.Any(c => c.Number == helper.ObjectIndex))
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




            Finish();
        }


        private void Finish()
        {
            ProgressValue = 100;
            TodoText = "Erfolgreich";
            Analytics.TrackEvent("Geräte Info auslesen");
            Finished?.Invoke(this, _data);
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
