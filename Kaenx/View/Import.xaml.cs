using Kaenx.Classes;
using Kaenx.Classes.Helper;
using Kaenx.DataContext.Catalog;
using Kaenx.MVVM;
using Microsoft.AppCenter.Analytics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class Import : Page, INotifyPropertyChanged
    {
        public ImportDevices Imports { get; set; }
        ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Import");

        private string _importState = "Starte...";
        public string ImportState
        {
            get { return _importState; }
            set { _importState = value; Update("ImportState"); }
        }

        private string _importDevice = "";
        public string ImportDevice
        {
            get { return _importDevice; }
            set { _importDevice = value; Update("ImportDevice"); }
        }

        public ObservableCollection<string> ImportError { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ImportWarning { get; set; } = new ObservableCollection<string>();

        private ObservableCollection<DeviceImportInfo> _devicesList = new ObservableCollection<DeviceImportInfo>();
        public ObservableCollection<DeviceImportInfo> DevicesList
        {
            get { return _devicesList; }
            set { _devicesList = value; Update("DevicesList"); }
        }

        private ImportHelper Helper = new ImportHelper();

        public Import()
        {
            this.InitializeComponent();
            this.DataContext = this;

            Helper.ProgressMaxChanged += Helper_ProgressMaxChanged;
            Helper.ProgressChanged += Helper_ProgressChanged;
            Helper.ProgressAppChanged += Helper_ProgressAppChanged;
            Helper.ProgressAppMaxChanged += Helper_ProgressAppMaxChanged;
            Helper.OnDeviceChanged += Helper_OnDeviceChanged;
            Helper.OnError += Helper_OnError;
            Helper.OnWarning += Helper_OnWarning;
            Helper.OnStateChanged += Helper_OnStateChanged;

            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        private void CurrentView_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (e.Handled) return;
            e.Handled = true;

            if (BtnBack.IsEnabled)
            {
                var currentView = SystemNavigationManager.GetForCurrentView();
                currentView.BackRequested -= CurrentView_BackRequested;
                ((Frame)this.Parent).Navigate(typeof(Catalog), Imports.wasFromMain ? "main" : null);
            }
        }

        private void Helper_OnWarning(string value)
        {
            ImportWarning.Add(value);
        }

        private void Helper_ProgressChanged(int count)
        {
            ProgressMain.IsIndeterminate = false;
            ProgressMain.Value = count;
        }

        private void Helper_ProgressMaxChanged(int count)
        {
            ProgressMain.IsIndeterminate = false;
            ProgressMain.Maximum = count;
        }

        #region Helper Events
        private void Helper_OnDeviceChanged(string value)
        {
            ImportDevice = value;
        }

        private void Helper_OnStateChanged(string value)
        {
            ImportState = value;
        }

        private void Helper_ProgressAppMaxChanged(int count)
        {
            ProgressApp.Maximum = count;
            ProgressApp.Value = 0;
        }

        private void Helper_ProgressAppChanged(int count)
        {
            ProgressApp.Value = count;
        }

        private void Helper_OnError(string Error)
        {
            ImportError.Add(Error);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Imports = (ImportDevices)e.Parameter;
            StartImport();
            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.BackRequested += CurrentView_BackRequested;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.BackRequested -= CurrentView_BackRequested;
        }
        #endregion



        private async void StartImport()
        {
            Log.Information("------------Import wurde gestartet------------");
            Log.Information("Sprache: " + Imports.SelectedLanguage);
            ImportState = resourceLoader.GetString("StateProj");
            IEnumerable<Device> devices = from dev in Imports.DeviceList where dev.SlideSettings.IsSelected == true select dev;

            foreach (Device device in devices)
            {
                DevicesList.Add(new DeviceImportInfo()
                {
                    Id = device.Id,
                    Name = device.Name,
                    Description = device.VisibleDescription
                });
            }

            ProgressMain.IsIndeterminate = true;

            Helper.Imports = Imports;



            //Vom Internet holen: https://update.knx.org/data/XML/project-11/knx_master.xml
            await Helper.StartImport(DevicesList);



            BtnBack.IsEnabled = true;
            ViewDevicesList.SelectedItem = null;
            Analytics.TrackEvent("Gerät(e) importiert");


            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            return;


            ImportState = resourceLoader.GetString("StateCheck");
            await Task.Delay(1000);
            List<DeviceViewModel> AddedDevices = await Helper.CheckDevices();

            if (AddedDevices.Count > 0)
            {
                Log.Information("Es wurden " + AddedDevices.Count + " Geräte hinzugefügt");
                await Task.Delay(1000);
                string addedString = "";
                foreach (DeviceViewModel device in AddedDevices)
                {
                    addedString += device.Name + " " + device.Id + Environment.NewLine;
                }
                ImportError.Add(addedString);
            }

        }


        private async void ShowError(string msg)
        {
            ImportState = "Es trat ein Fehler auf!";
            ImportDevice = msg;
            BtnBack.IsEnabled = true;

            Imports.Archive.Dispose();
            StorageFile file = await ApplicationData.Current.TemporaryFolder.GetFileAsync("temp.knxprod");
            await file.DeleteAsync();
        }



        public event PropertyChangedEventHandler PropertyChanged;
        private void Update(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ClickBack(object sender, RoutedEventArgs e)
        {
            ((Frame)this.Parent).Navigate(typeof(Catalog), Imports.wasFromMain ? "main" : null);
        }
    }
}
