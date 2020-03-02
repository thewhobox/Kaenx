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
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Core;
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

            Helper.ProgressChanged += Helper_ProgressChanged;
            Helper.ProgressMaxChanged += Helper_ProgressMaxChanged;
            Helper.ProgressAppChanged += Helper_ProgressAppChanged;
            Helper.ProgressAppMaxChanged += Helper_ProgressAppMaxChanged;
            Helper.OnDeviceChanged += Helper_OnDeviceChanged;
            Helper.OnError += Helper_OnError;


            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        #region Helper Events
        private void Helper_OnDeviceChanged(string value)
        {
            ImportDevice = value;
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
            Update("ImportError");
        }

        private void Helper_ProgressMaxChanged(int count)
        {
            //ProgSub.Maximum = count;
            //ProgSub.Value = 0;
        }

        private void Helper_ProgressChanged(int count)
        {
            //ProgSub.Value = count;
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
            IEnumerable<Device> devices = from dev in Imports.DeviceList where dev.SlideSettings.IsSelected == true select dev;

            foreach (Device device in devices)
            {
                DevicesList.Add(new DeviceImportInfo()
                {
                    Id = device.Id,
                    Name = device.Name,
                    Description = device.VisibleDescription,
                    ProductRefId = device.ProductRefId
                });
            }

            ProgressMain.IsIndeterminate = true;

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
            //await Task.Delay(2000);
            XElement manXML = XDocument.Load(entry.Open()).Root;
            StorageFile masterFile;

            try
            {
                masterFile = await ApplicationData.Current.LocalFolder.GetFileAsync("knx_master.xml");
            }
            catch(Exception e)
            {
                StorageFile defaultFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/knx_master.xml"));
                masterFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("knx_master.xml");
                await FileIO.WriteTextAsync(masterFile, await FileIO.ReadTextAsync(defaultFile));
                Log.Warning(e, "Es konnte keine KNX_Master Datei gefunden werden.");
            }


            XDocument masterXml;
            try
            {
                masterXml = XDocument.Load(await masterFile.OpenStreamForReadAsync());
            }
            catch (Exception e)
            {
                Imports.Archive.Dispose();
                ImportState = resourceLoader.GetString("StateFin");
                ImportError.Add(resourceLoader.GetString("MsgMasterError"));
                BtnBack.IsEnabled = true;
                Log.Error(e, "KNX_Master konnte geöffnet werden.");
                ShowError("Die KNX_Master Datei konnte nicht geöffnet werden.");
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
            catch (Exception e)
            {
                Log.Error(e, "KNX_Master konnte nicht aktualisiert werden.");
                ShowError("KNX_Master konnte nicht aktualisiert werden.");
                return;
            }


            ImportDevice = resourceLoader.GetString("StateManus");
            Helper.UpdateManufacturers(manXML);
            #endregion


            #region Katalog

            ImportDevice = resourceLoader.GetString("StateCat");
            Log.Information("---- Katalog analyse gestartet");
            //await Task.Delay(2000);
            entry = Imports.Archive.GetEntry(Helper.currentMan + "/Catalog.xml");
            XElement catalogXml = XDocument.Load(entry.Open()).Root;
            try
            {
                await ImportHelper.TranslateXml(catalogXml, Imports.SelectedLanguage);
                await Helper.ImportCatalog(catalogXml, DevicesList);
            }
            catch (Exception e)
            {
                Log.Error(e, "Katalog Fehler!");
            }
            Log.Information("Katalog wurde aktualisiert");

            #endregion


            ProgressMain.IsIndeterminate = true;

            List<string> loadedIds = new List<string>();





            foreach (DeviceImportInfo device in DevicesList)
            {
                ViewDevicesList.SelectedItem = device;
                ProgressMain.Value = 0;

                await Task.Delay(1000);

                ImportDevice = resourceLoader.GetString("StateHard");
                Log.Information("---- Hardware wird importiert");

                try
                {
                    entry = Imports.Archive.GetEntry(Helper.currentMan + "/Hardware.xml");
                    XElement xml = XDocument.Load(entry.Open()).Root;
                    await ImportHelper.TranslateXml(xml, Imports.SelectedLanguage);
                    Helper.ImportHardware(xml, device);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Hardware Fehler!");
                    ImportError.Add(device.ApplicationId + ": " + e.Message);
                    device.Icon = Symbol.ReportHacked;
                    continue;
                }
                await Task.Delay(2000);
                ProgressMain.Value += 1;

                if (!loadedIds.Contains(device.ApplicationId))
                {
                    ImportDevice = resourceLoader.GetString("StateApp");
                    Log.Information("---- Applikation wird importiert");
                    Log.Information(device.ApplicationId);

                    string manuId = device.ApplicationId.Substring(0, device.ApplicationId.IndexOf('_'));
                    ZipArchiveEntry appEntry = Imports.Archive.GetEntry(manuId + "/" + device.ApplicationId + ".xml");
                    List<string> errs = Helper.CheckApplication(XmlReader.Create(appEntry.Open()));

                    if (errs.Count > 0)
                    {
                        Log.Error("Check nicht bestanden! " + string.Join(",", errs));
                        ImportError.Add(device.ApplicationId + ": Die Applikation hat die Überprüfung nicht bestanden. (evtl. Plugin benötigt) " + string.Join(",", errs));
                        device.Icon = Symbol.ReportHacked;
                        continue;
                    }

                    await Task.Delay(1000);

                    try
                    {
                        XElement doc = XDocument.Load(appEntry.Open()).Root;
                        ImportDevice = "Übersetzen";
                        await ImportHelper.TranslateXml(doc, Imports.SelectedLanguage, Helper_ProgressAppMaxChanged, Helper_ProgressAppChanged);
                        XElement appXml = doc.Descendants(XName.Get("ApplicationProgram", doc.Name.Namespace.NamespaceName)).First();
                        ImportDevice = "Applikation importieren";
                        await Helper.ImportApplications(appXml, device);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Applikation Fehler!");
                        ImportError.Add(device.ApplicationId + ": " + e.Message);
                        device.Icon = Symbol.ReportHacked;
                        continue;
                    }
                    Log.Information("Import Applikationen abgeschlossen");
                    loadedIds.Add(device.ApplicationId);
                } else
                {

                }
                ProgressMain.Value += 1;


                ImportDevice = resourceLoader.GetString("StateCheck");
                await Task.Delay(2000);
                ProgressMain.Value += 1;

                device.Icon = Symbol.Like;
                await Task.Delay(2000);

            }


            Imports.Archive.Dispose();
            StorageFile file = await ApplicationData.Current.TemporaryFolder.GetFileAsync("temp.knxprod");
            await file.DeleteAsync();


            await Task.Delay(1000);
            ImportState = resourceLoader.GetString("StateFin");

            BtnBack.IsEnabled = true;
            Log.Information("Import abgeschlossen");

            Analytics.TrackEvent("Gerät(e) importiert");

            ViewDevicesList.SelectedItem = null;
            return;




            #region Hardware

            
            entry = Imports.Archive.GetEntry(Helper.currentMan + "/Hardware.xml");
            ImportDevice = resourceLoader.GetString("StateHard");
            Log.Information("---- Hardware wird importiert");
            try
            {
                XElement xml = XDocument.Load(entry.Open()).Root;
                await ImportHelper.TranslateXml(xml, Imports.SelectedLanguage);
                //await Helper.ImportHardware(xml, prod2load, catalogXml);
            }
            catch (Exception e)
            {
                Log.Error(e, "Hardware Fehler!");
                ShowError(e.Message);
                return;
            }
            Log.Information("Hardware wurde importiert");

            #endregion


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
