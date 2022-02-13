using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Import;
using Kaenx.DataContext.Import.Manager;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
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
    public sealed partial class Import : Page
    {
        public ObservableCollection<ImportDevice> ImportList { get; set; } = new ObservableCollection<ImportDevice>();

        private IManager manager;
        private CancellationTokenSource source;
        private bool isImporting = false;
        private bool navigatedFromMain = true;
        private ImportDevice currentDevice;

        public Import()
        {
            this.InitializeComponent();
            DataContext = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if(e.Parameter is StorageFile)
            {
                CopyFile(e.Parameter as StorageFile);
            } else if(e.Parameter is bool)
            {
                navigatedFromMain = (bool)e.Parameter;
                GetFile(null, null);
            }

            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.BackRequested += CurrentView_BackRequested;
        }

        private void CurrentView_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (e.Handled) return;
            e.Handled = true;

            if (!isImporting)
            {
                var currentView = SystemNavigationManager.GetForCurrentView();
                currentView.BackRequested -= CurrentView_BackRequested;
                ((Frame)this.Parent).Navigate(typeof(Catalog), navigatedFromMain ? "main" : null);
            }
        }

        private async void CopyFile(StorageFile file)
        {
            try
            {
                await file.CopyAsync(ApplicationData.Current.TemporaryFolder, file.Name, NameCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Fehler beim Kopieren der KNX-Prod Datei");
                return;
            }
            string path = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, file.Name);
            OpenFile(path);
        }

        private void OpenFile(string file)
        {
            manager = ImportManager.GetImportManager(file);
            manager.DeviceChanged += Manager_DeviceChanged;
            manager.StateChanged += Manager_StateChanged;
            var x = manager.GetDeviceList();
            ImportList.Clear();
            foreach (ImportDevice dev in x)
            {
                ImportList.Add(dev);
            }
        }

        private void Manager_DeviceChanged(string newName)
        {
            Serilog.Log.Debug($"Neues Gerät: {newName}");
        }

        private void Manager_StateChanged(string newName)
        {
            _=Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                currentDevice.Action = newName;
            });
            Debug.WriteLine(newName);
            Serilog.Log.Debug($"Aktion: {newName}");
        }

        private async void GetFile(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".knxprod");
            picker.FileTypeFilter.Add(".xml");
            StorageFile file = await picker.PickSingleFileAsync();
            if (file == null) return;
            try
            {
                await file.CopyAsync(ApplicationData.Current.TemporaryFolder, file.Name, NameCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Fehler beim Kopieren der KNX-Prod Datei");
                return;
            }
            string path = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, file.Name);

            OpenFile(path);
        }

        private async void Click_Start(object sender, RoutedEventArgs e)
        {
            //List.IsEnabled = false;
            List.SelectedItem = null;
            List.SelectionMode = ListViewSelectionMode.None;


            using (CatalogContext context = new CatalogContext())
            {
                foreach (ImportDevice dev in ImportList.Where(i => i.IsSelected))
                {
                    currentDevice = dev;
                    dev.State = ImportState.Importing;
                    try
                    {

                        await Task.Run(() =>
                        {
                            manager.StartImport(dev, context);
                        });
                        dev.State = ImportState.Finished;
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "Fehler beim Importieren");
                        dev.State = ImportState.Error;
                        dev.Action = ex.Message;

                        if(ex.InnerException != null)
                        {
                            dev.Action += " - " + ex.InnerException.Message;
                            Serilog.Log.Error(ex.InnerException, "Fehler beim Importieren");
                        }
                    }
                    
                }
            }
        }

        private void ImportItemClick(object sender, ItemClickEventArgs e)
        {
            ImportDevice device = e.ClickedItem as ImportDevice;
            device.IsSelected = !device.IsSelected;
        }
    }
}
