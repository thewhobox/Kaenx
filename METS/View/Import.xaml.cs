using METS.Classes;
using METS.Classes.Helper;
using METS.Context.Catalog;
using METS.MVVM;
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
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace METS.View
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

        private ObservableCollection<string> _importError = new ObservableCollection<string>();
        public ObservableCollection<string> ImportError
        {
            get { return _importError; }
            set { _importError = value; Update("ImportError"); }
        }

        private ImportHelper Helper = new ImportHelper();

        public Import()
        {
            this.InitializeComponent();
            this.DataContext = this;

            Helper.ProgressChanged += Helper_ProgressChanged;
            Helper.ProgressMaxChanged += Helper_ProgressMaxChanged;
            Helper.ProgressAppChanged += Helper_ProgressAppChanged;
            Helper.ProgressAppMaxChanged += Helper_ProgressAppMaxChanged;
            Helper.OnDeviceChanged += Helper_OnDeviceChanged;
            Helper.OnError += Helper_OnError;
        }

        #region Helper Events
        private void Helper_OnDeviceChanged(string value)
        {
            ImportDevice = value;
        }

        private void Helper_ProgressAppMaxChanged(int count)
        {
            ProgApp.Maximum = count;
            ProgApp.Value = 0;
        }

        private void Helper_ProgressAppChanged(int count)
        {
            ProgApp.Value = count;
        }

        private void Helper_OnError(string Error)
        {
            ImportError.Add(Error);
            Update("ImportError");
        }

        private void Helper_ProgressMaxChanged(int count)
        {
            ProgSub.Maximum = count;
            ProgSub.Value = 0;
        }

        private void Helper_ProgressChanged(int count)
        {
            ProgSub.Value = count;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Imports = (ImportDevices)e.Parameter;
            StartImport();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }
        #endregion



        private async void StartImport()
        {
            Log.Information("------------Import wurde gestartet------------");
            Log.Information("Sprache: " + Imports.SelectedLanguage);
            ImportState = resourceLoader.GetString("StateProj");
            List<string> prod2load = new List<string>();
            IEnumerable<Device> devices = from dev in Imports.DeviceList where dev.SlideSettings.IsSelected == true select dev;

            foreach (Device device in devices)
            {
                prod2load.Add(device.ProductRefId);
            }

            ProgSub.IsIndeterminate = true;

            foreach (ZipArchiveEntry entryT in Imports.Archive.Entries)
            {
                if (entryT.FullName.StartsWith("M-"))
                {
                    Helper.currentMan = entryT.FullName.Substring(0, 6);
                    break;
                }
            }


            //Vom Internet holen: https://update.knx.org/data/XML/project-11/knx_master.xml
            #region KNX_Master

            ImportDevice = "KNX-Master Datei aktualisieren";
            ZipArchiveEntry entry = Imports.Archive.GetEntry("knx_master.xml");
            Log.Information("---- Integrierte KNX_Master wird überprüft");
            await Task.Delay(2000);
            XElement manXML = XDocument.Load(entry.Open()).Root;
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


            XDocument masterXml;
            try
            {
                masterXml = XDocument.Load(await masterFile.OpenStreamForReadAsync());
            }
            catch (Exception e)
            {
                Log.Error(e, "KNX_Master laden Fehler!");
                Imports.Archive.Dispose();
                ImportState = resourceLoader.GetString("StateFin");
                ImportError.Add(resourceLoader.GetString("MsgMasterError"));
                BtnBack.IsEnabled = true;
                return;
            }

            string versionO = masterXml.Root.Element(XName.Get("MasterData", masterXml.Root.Name.NamespaceName)).Attribute("Version").Value;
            string versionN = manXML.Element(XName.Get("MasterData", manXML.Name.NamespaceName)).Attribute("Version").Value;

            int versionNew, versionOld;

            try
            {
                versionNew = int.Parse(versionN);
                versionOld = int.Parse(versionO);

                bool newer = versionNew > versionOld;

                if (newer)
                {
                    await FileIO.WriteTextAsync(masterFile, manXML.ToString());
                    Log.Information("KNX_Master wurde aktualisiert");
                }
            }
            catch { }


            ImportDevice = resourceLoader.GetString("StateManus");
            await Helper.UpdateManufacturers(manXML);
            #endregion


            #region Katalog

            ImportDevice = resourceLoader.GetString("StateCat");
            Log.Information("---- Katalog analyse gestartet");
            await Task.Delay(2000);
            entry = Imports.Archive.GetEntry(Helper.currentMan + "/Catalog.xml");
            try
            {
                XElement xml = XDocument.Load(entry.Open()).Root;
                ImportHelper.TranslateXml(xml, Imports.SelectedLanguage);
                await Helper.ImportCatalog(xml);
            }
            catch (Exception e)
            {
                Log.Error(e, "Katalog Fehler!");
            }
            ProgSub.Value += 1;
            Log.Information("Katalog wurde aktualisiert");

            #endregion


            #region Hardware

            entry = Imports.Archive.GetEntry(Helper.currentMan + "/Hardware.xml");
            ImportDevice = resourceLoader.GetString("StateHard");
            Log.Information("---- Hardware wird importiert");
            await Task.Delay(2000);
            try
            {
                XElement xml = XDocument.Load(entry.Open()).Root;
                ImportHelper.TranslateXml(xml, Imports.SelectedLanguage);
                await Helper.ImportHardware(xml, prod2load);
            }
            catch (Exception e)
            {
                Log.Error(e, "Hardware Fehler!");
            }
            Log.Information("Hardware wurde importiert");

            #endregion


            ProgMain.Value += 1;
            ProgSub.IsIndeterminate = false;
            ProgSub.Value = 0;

            await Task.Delay(2000);

            ImportDevice = "";
            Log.Information("---- Applikationen werden importiert");
            ImportState = resourceLoader.GetString("StateApp");
            await Task.Delay(1000);
            try
            {
                await Helper.ImportApplications(Imports);
            }
            catch (Exception e)
            {
                Log.Error(e, "Applikation Fehler!");
            }
            ProgMain.Value += 1;

            Log.Information("Import Applikationen abgeschlossen");



            Imports.Archive.Dispose();

            StorageFile file = await ApplicationData.Current.TemporaryFolder.GetFileAsync("temp.knxprod");
            await file.DeleteAsync();


            //ImportState = "Checke Applikationsprogramme...";
            //await Task.Delay(1000);
            //await Helper.CheckParams();
            ProgMain.Value += 1;

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


            await Task.Delay(1000);
            ImportState = resourceLoader.GetString("StateFin");
            ProgMain.Value += 1;

            BtnBack.IsEnabled = true;
            Log.Information("Import abgeschlossen");

            Analytics.TrackEvent("Gerät(e) importiert");
        }



        public event PropertyChangedEventHandler PropertyChanged;
        private void Update(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ClickBack(object sender, RoutedEventArgs e)
        {
            ((Frame)this.Parent).GoBack();
        }
    }
}
