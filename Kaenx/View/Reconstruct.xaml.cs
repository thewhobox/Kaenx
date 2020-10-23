using Kaenx.Classes;
using Kaenx.Classes.Bus;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Local;
using Kaenx.Konnect;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Connections;
using Kaenx.Konnect.Interfaces;
using Kaenx.View.Controls.Dialogs;
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
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
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

        private bool _cando;
        public bool CanDo
        {
            get { return _cando; }
            set { _cando = value; Changed("CanDo"); }
        }

        private int _progMax = 1;
        public int ProgMax
        {
            get { return _progMax; }
            set { _progMax = value; Changed("ProgMax"); }
        }

        private int _progValue = 1;
        public int ProgValue
        {
            get { return _progValue; }
            set { _progValue = value; Changed("ProgValue"); }
        }

        private bool _progIndet = false;
        public bool ProgIndet
        {
            get { return _progIndet; }
            set { _progIndet = value; Changed("ProgIndet"); }
        }


        public ObservableCollection<ReconstructDevice> Devices { get; set; } = new ObservableCollection<ReconstructDevice>();

        private Kaenx.DataContext.Project.ProjectContext _contextP;
        private Project _project;
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

        private void CurrentView_BackRequested(object sender, BackRequestedEventArgs e)
        {
            App.Navigate(typeof(MainPage));
            e.Handled = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _project = e.Parameter as Project;
            ApplicationView.GetForCurrentView().Title = _project.Name + " - Rekonstruktion";

            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            currentView.BackRequested += CurrentView_BackRequested;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.BackRequested -= CurrentView_BackRequested;
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



            foreach(Line linema in SaveHelper._project.Lines)
            {
                foreach(LineMiddle linemi in linema.Subs)
                {
                    foreach(LineDevice ldev in linemi.Subs)
                    {
                        ReconstructDevice dev = new ReconstructDevice();
                        dev.Address = UnicastAddress.FromString(ldev.LineName);
                        dev.Serial = ldev.SerialText;
                        string[] names = ldev.Name.Split("_");
                        dev.DeviceName = names[0];
                        dev.ApplicationName = names[1];
                        dev.ApplicationId = ldev.ApplicationId;
                        dev.StateId = 2;
                        dev.Status = "Gespeichert";
                        dev.Manufacturer = manus[dev.ApplicationId.Substring(0, 6)];
                        Devices.Add(dev);
                    }
                }
            }
            


            CanDo = true;
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
            CanDo = false;

            DiagSelectLines diag = new DiagSelectLines();
            await diag.ShowAsync();

            if (diag.Patterns.Count == 0)
            {
                CanDo = true;
                return;
            }

            List<UnicastAddress> addresses = new List<UnicastAddress>();

            foreach (SearchPattern pattern in diag.Patterns)
                foreach (UnicastAddress addr in pattern.GetAddresses())
                    if (!addresses.Any(a => a.ToString() == addr.ToString()))
                        addresses.Add(addr);

            ProgMax = addresses.Count;
            ProgValue = 0;


            //foreach (UnicastAddress addr in addresses)
            //{
            //    Action = $"Scanne Linie: {addr.Area}.{addr.Line}.x";
            //    await Task.Delay(100);
            //    ProgValue++;
            //}


            IKnxConnection _conn = KnxInterfaceHelper.GetConnection(conn.SelectedInterface);
            await _conn.Connect();
            _conn.OnTunnelRequest += _conn_OnTunnelRequest;



            foreach (UnicastAddress addr in addresses)
            {
                Action = $"Scanne Linie: {addr.Area}.{addr.Line}.x";
                BusDevice dev = new BusDevice(addr, _conn);
                await dev.Connect(true);
                ProgValue++;
            }

            Action = "Warten auf Geräte...";
            ProgIndet = true;
            await Task.Delay(15000);


            Action = "Lese Daten aus Geräten...";
            await Task.Delay(1000);

            ProgMax = Devices.Where(d => d.StateId == 0).Count();
            ProgValue = 0;

            while (true)
            {
                ProgValue++;

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
                    device.Status = "Fehler 0x01";
                    device.StateId = -1;
                    continue;
                }
                catch(Exception ex)
                {
                    device.Status = "Fehler 0x00";
                    device.StateId = -1;
                    Serilog.Log.Error(ex, "Fehler beim Auslesen von Gerät");
                    continue;
                }

                if (device.ApplicationId == null) continue;

                try
                {
                    await SaveDevice(device);
                }
                catch
                {
                    device.Status = "Fehler 0x04";
                    device.StateId = -1;
                }
            }


            Action = "Fertig!";
            await _conn.Disconnect();
            CanDo = true;
        }

        private async Task ReadDevice(ReconstructDevice device, IKnxConnection _conn)
        {
            device.StateId = 1;
            device.Status = "Lese Info...";
            BusDevice dev = new BusDevice(device.Address, _conn);
            await dev.Connect(true);

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

            appId = $"M-{appId.Substring(0, 4)}_A-{appId.Substring(4, 4)}-{appId.Substring(8, 2)}";

            try
            {
                Hardware2AppModel h2a = _context.Hardware2App.First(h => h.ApplicationId == appId);
                device.ApplicationName = h2a.Name;
                device.ApplicationId = h2a.ApplicationId;
                DeviceViewModel devm = _context.Devices.First(d => d.HardwareId == h2a.HardwareId);
                device.DeviceName = devm.Name;
            }
            catch
            {
                Debug.WriteLine(device.Address.ToString() + ": " + appId);
                if (string.IsNullOrEmpty(device.ApplicationName))
                    device.ApplicationName = "Fehler 0x03";
                device.DeviceName = "Fehler 0x03";
            }



            try
            {
                device.SerialBytes = await dev.PropertyRead(0,11);
                device.Serial = BitConverter.ToString(device.SerialBytes).Replace("-", "");
            }
            catch {
                device.Serial = "Fehler 0x02";
            }


            device.Status = "Infos gelesen";
        }

        private async Task SaveDevice(ReconstructDevice device)
        {
            Line line;
            if (SaveHelper._project.Lines.Any(l => l.Id == device.Address.Area))
            {
                line = SaveHelper._project.Lines.Single(l => l.Id == device.Address.Area);
            } else
            {
                line = new Line(device.Address.Area, "Neue Linie");
                SaveHelper._project.Lines.Add(line);
            }

            LineMiddle lineM;
            if (line.Subs.Any(l => l.Id == device.Address.Line))
                lineM = line.Subs.Single(l => l.Id == device.Address.Line);
            else
            {
                lineM = new LineMiddle(device.Address.Line, "Neue Linie", line);
                line.Subs.Add(lineM);
            }

            LineDevice lined;
            if (lineM.Subs.Any(d => d.Id == device.Address.DeviceAddress))
                lined = lineM.Subs.Single(d => d.Id == device.Address.DeviceAddress);
            else
            {
                lined = new LineDevice(true);
                lineM.Subs.Add(lined);
            }

            lined.Serial = device.SerialBytes;
            lined.ApplicationId = device.ApplicationId;
            lined.Id = device.Address.DeviceAddress;
            lined.Name = device.DeviceName + "_" + device.ApplicationName;

            device.StateId = 2;
            device.Status = "In Projekt";
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

        private void ClickReadStart(object sender, RoutedEventArgs e)
        {

        }

        private void ClickSave(object sender, RoutedEventArgs e)
        {
            SaveHelper.SaveProject();

            foreach (ReconstructDevice dev in Devices.Where(d => d.StateId == 2))
                dev.Status = "Gespeichert";
        }

        private void ClickToProject(object sender, RoutedEventArgs e)
        {
            SaveHelper.SaveProject();
            SaveHelper._project.Local.IsReconstruct = false;
            LocalContext con = new LocalContext();
            con.Projects.Update(SaveHelper._project.Local);
            con.SaveChanges();
            App.Navigate(typeof(WorkdeskEasy), SaveHelper._project);
        }
    }
}
