using Kaenx.Classes;
using Kaenx.Classes.Bus;
using Kaenx.DataContext.Catalog;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Classes;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class Reconstruct : Page, INotifyPropertyChanged
    {
        private string _action;
        public string Action
        {
            get { return _action; }
            set { _action = value; Changed("Action"); }
        }

        public ObservableCollection<ReconstructDevice> Devices { get; set; } = new ObservableCollection<ReconstructDevice>();

        private BusConnection conn = new BusConnection();
        private Dictionary<string, string> manus = new Dictionary<string, string>();
        private CatalogContext _context = new CatalogContext();
            

        public Reconstruct()
        {
            this.InitializeComponent();
            GridInterfaces.DataContext = conn;
            this.DataContext = this;
            Init();           
        }

        private async void Init()
        {
            StorageFile file = null;
            try
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync("knx_master.xml");
            }
            catch {
                StorageFile defaultFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/knx_master.xml"));
                file = await ApplicationData.Current.LocalFolder.CreateFileAsync("knx_master.xml");
                await FileIO.WriteTextAsync(file, await FileIO.ReadTextAsync(defaultFile));
            }

            if (file == null) return;

            XDocument master = XDocument.Load(await file.OpenStreamForReadAsync());

            foreach (XElement ele in master.Descendants(XName.Get("Manufacturer", master.Root.Name.NamespaceName)))
                manus.Add(ele.Attribute("Id").Value, ele.Attribute("Name").Value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ClickScanStart(object sender, RoutedEventArgs e)
        {
            if(conn.SelectedInterface == null)
            {
                Notify.Show("Bitte wählen Sie erst eine Schnittstelle aus.", 3000);
                return;
            }
            DoScan();
        }

        private async void DoScan()
        {
            Connection _conn = new Connection(conn.SelectedInterface.Endpoint);
            _conn.Connect();

            Action = "Scanne Linie: 1.1.x";

            _conn.OnTunnelRequest += _conn_OnTunnelRequest;

            for(int i = 0; i < 256; i++)
            {
                BusDevice dev = new BusDevice(UnicastAddress.FromString("1.1." + i), _conn);
                dev.Connect();
                await Task.Delay(50);
            }

            Action = "Warten auf Geräte...";
            await Task.Delay(15000);

            Action = "Lese Daten aus Geräten...";
            await Task.Delay(1000);


            while (true)
            {
                ReconstructDevice device;
                try
                {
                    device = Devices.First(d => d.StateId == 0);
                }
                catch { break; }

                try
                {
                    await ReadDevice(device, _conn);
                }
                catch (OperationCanceledException e)
                {
                    device.Status = "Fehler";
                    device.StateId = -1;
                }
                catch
                {

                }
            }


            _conn.Disconnect();
        }

        private async Task ReadDevice(ReconstructDevice device, Connection _conn)
        {
            device.StateId = 1;
            device.Status = "Lese Info...";
            BusDevice dev = new BusDevice(device.Address, _conn);
            dev.Connect();
            await Task.Delay(100);

            string mask = await dev.DeviceDescriptorRead();
            mask = "MV-" + mask;

            string appId = await dev.PropertyRead<string>(mask, "ApplicationId");

            if (appId.Length == 8)
            {
                appId = "00" + appId;
                device.Manufacturer = manus["M-" + appId.Substring(0, 4)];
            }
            else
                device.Manufacturer = manus["M-" + appId.Substring(0, 4)];

            appId = $"M-{appId.Substring(0, 4)}_A-{appId.Substring(4, 4)}-{appId.Substring(8, 2)}-";

            ApplicationViewModel model;
            try
            {
                model = _context.Applications.Single(a => a.Id.StartsWith(appId));
                device.ApplicationName = model.Name;
                Hardware2AppModel h2a = _context.Hardware2App.Single(h => h.ApplicationId.StartsWith(appId));
                DeviceViewModel devm = _context.Devices.First(d => d.HardwareId == h2a.HardwareId);
                device.DeviceName = devm.Name;
            }
            catch
            {
                if (string.IsNullOrEmpty(device.ApplicationName))
                    device.ApplicationName = "-";
                device.DeviceName = "-";
            }



            try
            {
                device.Serial = await dev.PropertyRead<string>(mask, "DeviceSerialNumber");
            }
            catch { device.Serial = "-"; }


            device.Status = "Infos gelesen";
        }

        private void _conn_OnTunnelRequest(Konnect.Builders.TunnelResponse response)
        {
            if(response.APCI == Konnect.Parser.ApciTypes.Disconnect)
            {
                if (Devices.Any(d => d.Address.ToString() == response.SourceAddress.ToString())) return;

                ReconstructDevice dev = new ReconstructDevice();
                dev.Address = response.SourceAddress;
                dev.Status = "Gefunden";
                _= App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Devices.Add(dev);
                });
            }
        }
    }
}
