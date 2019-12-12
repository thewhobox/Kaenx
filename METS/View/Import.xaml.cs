using METS.Classes;
using METS.Classes.Helper;
using METS.Context;
using METS.Context.Catalog;
using METS.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Resources;
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
            ImportState = resourceLoader.GetString("StateProj");
            string currentMan;
            await Task.Delay(1000);

            List<string> prod2load = new List<string>();
            IEnumerable<Device> devices = from dev in Imports.DeviceList where dev.SlideSettings.IsSelected == true select dev;

            foreach(Device device in devices)
            {
                prod2load.Add(device.ProductRefId);
            }

            bool manwasset = false;
            foreach (ZipArchiveEntry entry in Imports.Archive.Entries)
            {
                if (!manwasset && entry.FullName.StartsWith("M-"))
                {
                    Helper.currentMan = entry.FullName.Substring(0, 6);
                    manwasset = true;
                    continue;
                }

                if (entry.Name == "knx_master.xml")
                {
                    ImportState = resourceLoader.GetString("StateManus");
                    await Task.Delay(1000);

                    XElement manXML = XDocument.Load(entry.Open()).Root;

                    await Helper.UpdateManufacturers(manXML);

                    StorageFile masterFile;

                    try
                    {
                        masterFile = await ApplicationData.Current.LocalFolder.GetFileAsync("knx_master.xml");
                    } catch
                    {
                        StorageFile defaultFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/knx_master.xml"));
                        masterFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("knx_master.xml");
                        await FileIO.WriteTextAsync(masterFile, await FileIO.ReadTextAsync(defaultFile));
                    }

                    
                    XDocument masterXml = XDocument.Load(await masterFile.OpenStreamForReadAsync());

                    string versionO = masterXml.Root.Element(XName.Get("MasterData", masterXml.Root.Name.NamespaceName)).Attribute("Version").Value;
                    string versionN = manXML.Element(XName.Get("MasterData", masterXml.Root.Name.NamespaceName)).Attribute("Version").Value;

                    int versionNew, versionOld;

                    try
                    {
                        versionNew = int.Parse(versionN);
                        versionOld = int.Parse(versionO);

                        bool newer = versionNew > versionOld;

                        if(newer)
                        {
                            StreamWriter sw = new StreamWriter(await masterFile.OpenStreamForWriteAsync());
                            sw.Write(manXML.ToString());
                        }
                    } catch { }

                    ProgSub.Value = 0;
                    ProgMain.Value += 1;
                    continue;
                }

                if (entry.Name == "Catalog.xml")
                {
                    currentMan = entry.FullName.Substring(0, entry.FullName.IndexOf('/'));
                    ImportState = resourceLoader.GetString("StateCat");
                    await Task.Delay(1000);
                    await Helper.ImportCatalog(entry);
                    ProgSub.Value = 0;
                    ProgMain.Value += 1;
                    continue;
                }

                if (entry.Name == "Hardware.xml")
                {
                    currentMan = entry.FullName.Substring(0, entry.FullName.IndexOf('/'));
                    ImportState = resourceLoader.GetString("StateHard");
                    await Task.Delay(1000);
                    await Helper.ImportHardware(entry, prod2load);
                    ProgSub.Value = 0;
                    ProgMain.Value += 1;
                    continue;
                }
            }

            ImportState = resourceLoader.GetString("StateApp");
            await Task.Delay(1000);
            await Helper.ImportApplications(Imports.Archive);
            ProgMain.Value += 1;




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
                await Task.Delay(1000);
                string addedString = "{ \"type\": \"added\", \"list\": [   ";
                foreach (DeviceViewModel device in AddedDevices)
                {
                    addedString += "\r\n { \"Id\": \"" + device.Id + "\", \"Text\": \"" + device.Name + "\" }, ";
                }
                addedString = addedString.Substring(0, addedString.Length - 2);
                addedString += "\r\n ] }";
                ImportError.Add(addedString);
            }


            await Task.Delay(1000);
            ImportState = resourceLoader.GetString("StateFin");
            ProgMain.Value += 1;

            BtnBack.IsEnabled = true;
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
