using Kaenx.Classes.Bus.Data;
using Kaenx.DataContext.Catalog;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Classes;
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
        private CancellationToken _token;

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
            BusDevice dev = new BusDevice(Device.LineName, Connection);
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

            try
            {
                Hardware2AppModel h2a = context.Hardware2App.First(h => h.ApplicationId.StartsWith(appId));
                DeviceViewModel dvm = context.Devices.First(d => d.HardwareId == h2a.HardwareId);
                _data.ApplicationName = h2a.Name + " " + h2a.VersionString;
            }
            catch { }

            #endregion

            
            byte[] mem = await dev.MemoryRead(17408, 304);

            _data.Memory = mem;

            Finish();
        }


        private void Finish()
        {
            ProgressValue = 100;
            TodoText = "Erfolgreich";
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
