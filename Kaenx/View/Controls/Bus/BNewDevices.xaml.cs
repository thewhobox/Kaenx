using Kaenx.Classes;
using Kaenx.Classes.Bus;
using Kaenx.Classes.Bus.Data;
using Kaenx.DataContext.Catalog;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Classes;
using Kaenx.View.Controls.Dialogs;
using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace Kaenx.View.Controls.Bus
{
    public sealed partial class BNewDevices : UserControl
    {
        public ObservableCollection<NewDeviceData> DeviceList { get; } = new ObservableCollection<NewDeviceData>();

        private Connection conn;
        private bool isReading = false;

        public BNewDevices()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private void Conn_OnTunnelResponse(Konnect.Builders.TunnelResponse response)
        {
            if (response.APCI == Konnect.Parser.ApciTypes.PropertyValueResponse)
            {
                byte[] serialb = response.Data.Skip(4).ToArray();
                string serial = BitConverter.ToString(serialb).Replace("-", "");
                if (!DeviceList.Any(d => d.SerialText == serial))
                {
                    _= App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        DeviceList.Add(new NewDeviceData() { Serial = serialb });
                    });
                }
                    
                Debug.WriteLine("Neues Gerät: " + serial);
            } else
            {

            }
        }

        private async void ClickSearch(object sender, RoutedEventArgs e)
        {
            BtnSearch.IsEnabled = false;

            if (conn?.IsConnected == true)
                conn.Disconnect();

            conn = new Connection(BusConnection.Instance.SelectedInterface.Endpoint);
            conn.OnTunnelResponse += Conn_OnTunnelResponse;
            conn.Connect();
            await Task.Delay(200);

            //BusCommon comm = new BusCommon(conn);
            BusDevice dev = new BusDevice(UnicastAddress.FromString("15.15.255"), conn);
            dev.Connect();
            await Task.Delay(100);

            _ = dev.PropertyRead<string>(0, 11);

            await Task.Delay(2000);
            dev.Disconnect();
            await Task.Delay(200);

            if (!isReading)
                ReadInfos();

            BtnSearch.IsEnabled = true;

            Analytics.TrackEvent("Neue Geräte gesucht");
        }


        private async void ReadInfos()
        {
            BusCommon comm = new BusCommon(conn);

            CatalogContext _context = new CatalogContext();

            while (DeviceList.Any(d => d.finished == false))
            {
                NewDeviceData data = DeviceList.First(d => d.finished == false);
                data.Status = "Wird gelesen...";

                Debug.WriteLine("Ändere Gerät: " + data.SerialText);
                comm.IndividualAddressWrite(UnicastAddress.FromString("15.15.254"), data.Serial);
                

                Debug.WriteLine("Disconnecting");
                await Task.Delay(4000);



                Debug.WriteLine("New Connection");

                Connection conn2 = new Connection(BusConnection.Instance.SelectedInterface.Endpoint);
                conn2.Connect();
                await Task.Delay(100);

                BusDevice dev = new BusDevice(UnicastAddress.FromString("15.15.254"), conn2);
                dev.Connect();

                await Task.Delay(100);


                string mask = "MV-" + await dev.DeviceDescriptorRead();

                string appId = await dev.PropertyRead<string>(mask, "ApplicationId");
                if (appId.Length == 8) appId = "00" + appId;
                data.ApplicationId = "M-" + appId.Substring(0, 4) + "_A-" + appId.Substring(4, 4) + "-" + appId.Substring(8, 2) + "-";

                XElement master = (await GetKnxMaster()).Root;
                XElement manu = master.Descendants(XName.Get("Manufacturer", master.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == "M-" + appId.Substring(0, 4));
                data.Manufacturer = manu.Attribute("Name").Value;

                try
                {
                    Hardware2AppModel h2a = _context.Hardware2App.Single(h => h.ApplicationId.StartsWith(data.ApplicationId));

                    List<string> names = new List<string>();
                    foreach (DeviceViewModel model in _context.Devices.Where(d => d.HardwareId == h2a.HardwareId))
                    {
                        names.Add(model.Name);
                        data.DeviceModels.Add(model);
                    }
                    data.DeviceName = string.Join(", ", names);
                }
                catch
                {
                    data.DeviceName = "Unbekannt";
                }


                dev.Disconnect();

                comm.IndividualAddressWrite(UnicastAddress.FromString("15.15.255"), data.Serial);
                data.Status = "Fertig";
                data.finished = true;
            }

            conn.Disconnect();
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


        private async void ClickIntegrate(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menu = sender as MenuFlyoutItem;
            NewDeviceData data = menu.DataContext as NewDeviceData;

            DiagImportDevice diag = new DiagImportDevice(data);

            await diag.ShowAsync();
        }

        private void MenuFlyout_Opening(object sender, object e)
        {
            MenuFlyout menu = sender as MenuFlyout;
            NewDeviceData data = (menu.Target as FrameworkElement).DataContext as NewDeviceData;

            (menu.Items[0] as MenuFlyoutItem).IsEnabled = data.finished;
        }
    }
}
