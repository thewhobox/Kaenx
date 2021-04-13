using Kaenx.Classes;
using Kaenx.Classes.Controls;
using Kaenx.DataContext;
using Kaenx.Classes.Helper;
using Kaenx.MVVM;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Collections.Generic;
using Windows.UI.Xaml.Navigation;
using Kaenx.DataContext.Catalog;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Kaenx.View.Controls;
using Windows.UI.Core;
using Windows.ApplicationModel.Resources;
using Windows.UI.ViewManagement;
using Kaenx.DataContext.Export;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Kaenx.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Catalog : Page, INotifyPropertyChanged
    {
        private string lastCategorie = "main";
        private ResourceLoader loader = ResourceLoader.GetForCurrentView("Catalog");
        private ObservableCollection<DeviceViewModel> _items = new ObservableCollection<DeviceViewModel>();
        private ObservableCollection<DeviceViewModel> _catalogDevices = new ObservableCollection<DeviceViewModel>();
        public ObservableCollection<DeviceViewModel> CatalogDevices
        {
            get { return _catalogDevices; }
            set { _catalogDevices = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CatalogDevices")); }
        }
        public ImportDevices Import { get; set; }

        private CatalogContext _context = new CatalogContext();

        public event PropertyChangedEventHandler PropertyChanged;

        public Catalog()
        {
            this.InitializeComponent();

            Import = new ImportDevices();

            LoadDevices("main");


            var mainNode = new Classes.TVNode();
            mainNode.Content = loader.GetString("MansListAll");
            mainNode.SectionId = "main";
            mainNode.IsExpanded = true;

            TreeV.RootNodes.Add(mainNode);


            LoadSections(mainNode.SectionId, mainNode);
            
            this.DataContext = this;


        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if(e.Parameter is StorageFile)
            {
                PrepareImport(e.Parameter as StorageFile);
                Import.wasFromMain = true;
                ApplicationView.GetForCurrentView().Title = loader.GetString("WindowTitle");

                var currentView = SystemNavigationManager.GetForCurrentView();
                currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                currentView.BackRequested += CurrentView_BackRequested;
            } else if(e.Parameter is string && e.Parameter.ToString() == "main") 
            {
                Import.wasFromMain = true;
                ApplicationView.GetForCurrentView().Title = loader.GetString("WindowTitle");

                var currentView = SystemNavigationManager.GetForCurrentView();
                currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                currentView.BackRequested += CurrentView_BackRequested;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.BackRequested -= CurrentView_BackRequested;
        }

        private void CurrentView_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (e.Handled) return;

            e.Handled = true;
            App.Navigate(typeof(MainPage));
        }

        private async void ClickImport(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".knxprod");
            StorageFile file = await picker.PickSingleFileAsync();
            PrepareImport(file);
        }

        public async void PrepareImport(StorageFile file, bool changeLang = false)
        {
            if (file == null) return;

            try
            {    
                await file.CopyAsync(ApplicationData.Current.TemporaryFolder, "temp.knxprod", NameCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                string msg = loader.GetString("MsgNotCopied");
                Serilog.Log.Error(ex, "Fehler beim Kopieren der KNX-Prod Datei");
                //Add notify
                Notifi.Show(msg + "\r\n" + ex.Message);
                return;
            }
            
            StorageFile file2 = await ApplicationData.Current.TemporaryFolder.GetFileAsync("temp.knxprod");
            Import.Archive = ZipFile.Open(file2.Path, ZipArchiveMode.Read);
            ImportHelper helper = new ImportHelper();
            bool success = await helper.GetDeviceList(Import);

            if (!success)
            {
                //todo blabla
                ViewHelper.Instance.ShowNotification("main", "Es trat ein Fehler beim auslesen der Geräte auf.", 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                return;
            }

            GridImportDevices.Visibility = Visibility.Visible;

            if (!string.IsNullOrEmpty(Import.SelectedLanguage))
                OutSelectedLang.Text = new System.Globalization.CultureInfo(Import.SelectedLanguage).DisplayName;

        }

        private async void ClickCancel(object sender, RoutedEventArgs e)
        {
            Import.Archive.Dispose();
            GridImportDevices.Visibility = Visibility.Collapsed;
            try
            {
                StorageFile file = await ApplicationData.Current.TemporaryFolder.GetFileAsync("temp.knxprod");
                await file.DeleteAsync();
            }
            catch { }
        }

        private void ClickSelectAll(object sender, RoutedEventArgs e)
        {
            foreach(Kaenx.Classes.Device device in Import.DeviceList)
            {
                device.SlideSettings.IsSelected = true;
            }

            Frame rootFrame = this.Parent as Frame;
            rootFrame.Navigate(typeof(Import), Import);
        }

        private void ClickSelected(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = this.Parent as Frame;
            rootFrame.Navigate(typeof(Import), Import);
        }

        private void TreeV_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            Classes.TVNode node = (Classes.TVNode)args.InvokedItem;
            LoadDevices(node.SectionId);
        }

        private async void LoadDevices(string section)
        {
            lastCategorie = section;
            CatalogDevices.Clear();
            _items.Clear();
            List<string> cats = new List<string>();
            cats.Add(section);
            await Task.Delay(290);

            GetSubSection(cats, section);

            foreach(DeviceViewModel model in _context.Devices.Where(dev => cats.Contains(dev.CatalogId)).ToList().OrderBy(dev => dev.Name))
            {
                CatalogDevices.Add(model);
                _items.Add(model);
            }
        }

        private void GetSubSection(List<string> list, string section)
        {
            IEnumerable<CatalogViewModel> sections = _context.Sections.Where(sec => sec.ParentId == section);
            foreach (CatalogViewModel cat in sections)
            {
                if (!list.Contains(cat.Id)) list.Add(cat.Id);
                GetSubSection(list, cat.Id);
            }
        }

        private void LoadSections(string section, TreeViewNode node)
        {
            IEnumerable<CatalogViewModel> sections = _context.Sections.Where(sec => sec.ParentId == section);

            foreach(CatalogViewModel sec in sections)
            {
                var secNode = new Classes.TVNode();
                secNode.Content = sec.Name;
                secNode.SectionId = sec.Id;
                node.Children.Add(secNode);
                LoadSections(sec.Id, secNode);
            }
        }

        private void CatalogDeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeviceViewModel device = (DeviceViewModel)((DataGrid)sender).SelectedItem;
            if(device == null || !device.HasApplicationProgram)
            {
                DevInfoApp.Text = "";
                BarDelete.IsEnabled = device != null;
                return;
            }
            BarDelete.IsEnabled = true;

            List<string> apps = new List<string>();
            IEnumerable<Hardware2AppModel> models = _context.Hardware2App.Where(h => h.HardwareId == device.HardwareId).OrderByDescending(h => h.Version);

            foreach(Hardware2AppModel model in models)
            {
                apps.Add($"{model.Name} {model.VersionString}");
            }
            DevInfoApp.Text = string.Join(Environment.NewLine, apps);
        }

        private DataGridColumn previousSortedColumn;

        private void CatalogDeviceList_Sorting(object sender, DataGridColumnEventArgs e)
        {
            if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Descending)
            {
                if (e.Column.Tag.ToString() == "Name")
                    CatalogDevices = new ObservableCollection<DeviceViewModel>(from item in CatalogDevices orderby item.Name ascending select item);
                if (e.Column.Tag.ToString() == "Desc")
                    CatalogDevices = new ObservableCollection<DeviceViewModel>(from item in CatalogDevices orderby item.VisibleDescription ascending select item);
                if (e.Column.Tag.ToString() == "OrderNr")
                    CatalogDevices = new ObservableCollection<DeviceViewModel>(from item in CatalogDevices orderby item.OrderNumber ascending select item);
                e.Column.SortDirection = DataGridSortDirection.Ascending;
            }
            else
            {
                if (e.Column.Tag.ToString() == "Name")
                    CatalogDevices = new ObservableCollection<DeviceViewModel>(from item in CatalogDevices orderby item.Name descending select item);
                if (e.Column.Tag.ToString() == "Desc")
                    CatalogDevices = new ObservableCollection<DeviceViewModel>(from item in CatalogDevices orderby item.VisibleDescription descending select item);
                if (e.Column.Tag.ToString() == "OrderNr")
                    CatalogDevices = new ObservableCollection<DeviceViewModel>(from item in CatalogDevices orderby item.OrderNumber descending select item);
                e.Column.SortDirection = DataGridSortDirection.Descending;
            }
            if(previousSortedColumn != null && previousSortedColumn != e.Column)
                previousSortedColumn.SortDirection = null;
            previousSortedColumn = e.Column;
        }

        private void SearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (previousSortedColumn != null)
                previousSortedColumn.SortDirection = null;

            string query = BarSearchIn.Text.ToLower();
            CatalogDevices = new ObservableCollection<DeviceViewModel>(from item in _items where contains(item.Name.ToLower(), query) || contains(item.OrderNumber.ToLower(), query) || (item.VisibleDescription != null && contains(item.VisibleDescription.ToLower(), query)) select item);
        }

        private bool contains(string input, string query)
        {
            if (query.Contains("{") || query.Contains("[") || query.Contains("(") || query.Contains("*"))
            {
                try
                {
                    Regex reg = new Regex(query);
                    BarSearchIn.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray);
                    return reg.IsMatch(input);
                }
                catch
                {
                    BarSearchIn.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Red);
                    return false;
                }
            } else
            {
                BarSearchIn.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray);
                return input.Contains(query);
            }
        }

        private void ClickDelete(object sender, RoutedEventArgs e)
        {
            foreach(DeviceViewModel device in CatalogDeviceList.SelectedItems)
            {
                _context.Devices.Remove(device);

                if (device.HasApplicationProgram)
                {
                    int count = _context.Devices.Count(d => d != device && d.HardwareId == device.HardwareId);
                    if (count == 0)
                    {
                        Hardware2AppModel h2a = _context.Hardware2App.Single(h => h.HardwareId == device.HardwareId);
                        _context.Hardware2App.Remove(h2a);
                        count = _context.Hardware2App.Count(h => h != h2a && h.ApplicationId == h2a.ApplicationId);

                        if (count == 0)
                        {
                            IEnumerable<object> tempList = _context.AppSegments.Where(a => a.ApplicationId == h2a.ApplicationId);
                            _context.RemoveRange(tempList);

                            tempList = _context.AppComObjects.Where(a => a.ApplicationId == h2a.ApplicationId);
                            _context.RemoveRange(tempList);

                            tempList = _context.Applications.Where(a => a.Id == h2a.ApplicationId);
                            _context.RemoveRange(tempList);

                            tempList = _context.AppParameters.Where(a => a.ApplicationId == h2a.ApplicationId);
                            _context.RemoveRange(tempList);

                            List<AppParameterTypeViewModel> toDelete = new List<AppParameterTypeViewModel>();
                            foreach (AppParameter para in tempList)
                            {
                                AppParameterTypeViewModel pType = _context.AppParameterTypes.Single(p => p.Id == para.ParameterTypeId);
                                toDelete.Add(pType);

                                if (pType.Type == ParamTypes.Enum)
                                {
                                    IEnumerable<AppParameterTypeEnumViewModel> tempList2 = _context.AppParameterTypeEnums.Where(e => e.ParameterId == pType.Id);
                                    _context.AppParameterTypeEnums.RemoveRange(tempList2);
                                }
                            }
                            _context.AppParameterTypes.RemoveRange(toDelete);

                            try
                            {
                                AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == h2a.ApplicationId);
                                _context.AppAdditionals.Remove(adds);
                            }
                            catch
                            {

                            }
                        }
                    }
                }
            }

            _context.SaveChanges();
            LoadDevices(lastCategorie);
        }

        private async void HyperlinkChangeLang_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            Import.SelectedLanguage = null;
            ImportHelper helper = new ImportHelper();
            await helper.GetDeviceList(Import, true);
        }

        private void ImportList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Kaenx.Classes.Device)
            {
                Kaenx.Classes.Device device = (Kaenx.Classes.Device)e.ClickedItem;
                device.SlideSettings.IsSelected = !device.SlideSettings.IsSelected;

                int count = Import.DeviceList.Where<Kaenx.Classes.Device>(d => d.SlideSettings.IsSelected == true).Count();

                if (count == 0)
                    ButtonImportSelected.IsEnabled = false;
                else
                    ButtonImportSelected.IsEnabled = true;
            }
        }

        private async void ClickExport(object sender, RoutedEventArgs e)
        {
            //FileSavePicker picker = new FileSavePicker();
            //picker.SuggestedFileName = (CatalogDeviceList.SelectedItems[0] as DeviceViewModel).Name;
            //picker.FileTypeChoices.Clear();
            //picker.FileTypeChoices.Add("Kaenx Devices", new List<string>() { ".kdev" });
            //StorageFile file = await picker.PickSaveFileAsync();


            //DevicesExport export = new DevicesExport();
            //CatalogContext context = new CatalogContext();

            //foreach (DeviceViewModel device in CatalogDeviceList.SelectedItems)
            //{
            //    export.Devices.Add(device);

            //    CatalogViewModel cat = context.Sections.Single(s => s.Id == device.CatalogId);
            //    export.Catalog.Add(cat);
            //    while (cat.ParentId != "main")
            //    {
            //        cat = context.Sections.Single(s => s.Id == cat.ParentId);
            //        export.Catalog.Add(cat);
            //    }

            //    foreach(Hardware2AppModel h2d in context.Hardware2App.Where(h => h.HardwareId == device.HardwareId))
            //    {
            //        export.Hard2App.Add(h2d);
            //        ApplicationViewModel appm = context.Applications.Single(a => a.Id == h2d.ApplicationId);
            //        export.Apps.Add(appm);

            //        export.Parameters.AddRange(context.AppParameters.Where(p => p.ApplicationId == appm.Id));
            //        export.ComObjects.AddRange(context.AppComObjects.Where(c => c.ApplicationId == appm.Id));
            //        export.ParamTypes.AddRange(context.AppParameterTypes.Where(t => t.ApplicationId == appm.Id));
            //        foreach(AppParameterTypeViewModel model in context.AppParameterTypes.Where(t => t.ApplicationId == appm.Id && t.Type == ParamTypes.Enum))
            //        {
            //            export.Enums.AddRange(context.AppParameterTypeEnums.Where(e => e.ParameterId == model.Id));
            //        }
            //    }
            //}

            //await FileIO.WriteTextAsync(file, "KaenxDev" + Newtonsoft.Json.JsonConvert.SerializeObject(export));
        }
    }
}