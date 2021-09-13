using Kaenx.Classes;
using Kaenx.Classes.Bus;
using Kaenx.Classes.Bus.Data;
using Kaenx.Classes.Helper;
using Kaenx.DataContext.Catalog;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Connections;
using Kaenx.Konnect.Interfaces;
using Kaenx.Konnect.Messages;
using Kaenx.Konnect.Messages.Response;
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

        private IKnxConnection conn;
        private bool isReading = false;

        public BNewDevices()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private void Conn_OnTunnelResponse(IMessageResponse response)
        {
            if (response is MsgPropertyReadRes)
            {
                MsgPropertyReadRes resp = response as MsgPropertyReadRes;

                if (resp.ObjectIndex == 0 && resp.PropertyId == 11)
                {
                    byte[] serialb = response.Raw.Skip(4).ToArray();
                    string serial = BitConverter.ToString(serialb).Replace("-", "");
                    if (!DeviceList.Any(d => d.SerialText == serial))
                    {
                        _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            DeviceList.Add(new NewDeviceData() { Serial = serialb });
                        });
                    }

                    Debug.WriteLine("Neues Gerät: " + serial);
                }
            } else
            {

            }
        }

        private async void ClickSearch(object sender, RoutedEventArgs e)
        {
            BtnSearch.IsEnabled = false;
            BtnSearch.Content = "Suche läuft...";

            if (conn?.IsConnected == true)
                await conn.Disconnect();

            if(BusConnection.Instance.SelectedInterface == null)
            {
                ViewHelper.Instance.ShowNotification("main", "Bitte wählen Sie erst eine Schnittstelle aus", 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                return;
            }

            conn = await KnxInterfaceHelper.GetConnection(BusConnection.Instance.SelectedInterface, BusRemoteConnection.Instance.Remote, BusConnection.Instance.GetDevice);
            conn.OnTunnelResponse += Conn_OnTunnelResponse;
            try
            {
                await conn.Connect();
            }catch(Exception ex)
            {
                ViewHelper.Instance.ShowNotification("main", "Probleme beim Verbinden mit der Schnittstelle!\r\n" + ex.Message, 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                return;
            }

            //BusCommon comm = new BusCommon(conn);
            BusDevice dev = new BusDevice(UnicastAddress.FromString("15.15.255"), conn);
            await dev.Connect(true);

            _ = dev.PropertyRead<string>(0, 11);

            await Task.Delay(2000);
            await dev.Disconnect();

            if (!isReading)
                await ReadInfos();

            await conn .Disconnect();

            BtnSearch.IsEnabled = true;
            BtnSearch.Content = "Suche starten";

            Analytics.TrackEvent("Neue Geräte gesucht");
        }


        private async Task ReadInfos()
        {
            BusCommon comm = new BusCommon(conn);

            CatalogContext _context = new CatalogContext();

            while (DeviceList.Any(d => d.finished == false))
            {
                NewDeviceData data = DeviceList.First(d => d.finished == false);
                data.Status = "Wird gelesen...";

                Debug.WriteLine("Ändere Gerät: " + data.SerialText);
                await comm .IndividualAddressWrite(UnicastAddress.FromString("15.15.254"), data.Serial);
                

                Debug.WriteLine("New Connection");

                //IKnxConnection conn2 = await KnxInterfaceHelper.GetConnection(BusConnection.Instance.SelectedInterface, BusRemoteConnection.Instance.Remote, BusConnection.Instance.GetDevice);
                //await conn2.Connect();

                BusDevice dev = new BusDevice(UnicastAddress.FromString("15.15.254"), conn);
                await dev.Connect(true);

                string appId = await dev.ResourceRead<string>("ApplicationId");
                if (appId.Length == 8) appId = "00" + appId;
                //data.ApplicationId = "M-" + appId.Substring(0, 4) + "_A-" + appId.Substring(4, 4) + "-" + appId.Substring(8, 2) ;

                XElement master = (await GetKnxMaster()).Root;
                XElement manu = master.Descendants(XName.Get("Manufacturer", master.Name.NamespaceName)).Single(m => m.Attribute("Id").Value == "M-" + appId.Substring(0, 4));
                data.Manufacturer = manu.Attribute("Name").Value;

                try
                {
                    Hardware2AppModel h2a = null; //TODO richtig machen _context.Hardware2App.Single(h => h.ApplicationId == data.ApplicationId);

                    List<string> names = new List<string>();
                    foreach (DeviceViewModel model in _context.Devices.Where(d => d.HardwareId == h2a.Id))
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


                await dev.Disconnect();

                await comm.IndividualAddressWrite(UnicastAddress.FromString("15.15.255"), data.Serial);
                data.Status = "Fertig";
                data.finished = true;
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
