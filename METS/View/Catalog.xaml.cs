using METS.Classes;
using METS.Classes.Controls;
using METS.Context;
using METS.Classes.Helper;
using METS.MVVM;
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
using METS.Context.Catalog;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using METS.View.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace METS.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Catalog : Page, INotifyPropertyChanged
    {
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
            mainNode.Content = "Alle Hersteller";
            mainNode.SectionId = "main";
            mainNode.IsExpanded = true;

            TreeV.RootNodes.Add(mainNode);


            LoadSections(mainNode.SectionId, mainNode);
            
            this.DataContext = this;
        }
     
        private async void ClickImport(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".knxprod");
            StorageFile file = await picker.PickSingleFileAsync();
            if (file == null) return;

            try
            {    
                await file.CopyAsync(ApplicationData.Current.TemporaryFolder, "temp.knxprod", NameCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView("Catalog");
                string msg = resourceLoader.GetString("MsgNotCopied");
                Notifi.Show(msg + "\r\n" + ex.Message);
                return;
            }
            
            StorageFile file2 = await ApplicationData.Current.TemporaryFolder.GetFileAsync("temp.knxprod");

            Import.Archive = ZipFile.Open(file2.Path, ZipArchiveMode.Read);

            foreach(ZipArchiveEntry entry in Import.Archive.Entries)
            {
                if(entry.FullName.StartsWith("M-") && entry.FullName.EndsWith("/Catalog.xml"))
                {
                    XDocument catXML = XDocument.Load(entry.Open());
                    string ns = catXML.Root.Name.NamespaceName;
                    List<XElement> langs = catXML.Descendants(XName.Get("Language", ns)).ToList();

                    ObservableCollection<string> tempLangs = new ObservableCollection<string>();
                    foreach(XElement lang in langs)
                    {
                        tempLangs.Add(lang.Attribute("Identifier").Value);
                    }

                    if(tempLangs.Count > 1)
                    {
                        DiagLanguage diaglang = new DiagLanguage(tempLangs);
                        await diaglang.ShowAsync();
                        Import.SelectedLanguage = diaglang.SelectedLanguage;
                        ImportHelper.TranslateXml(catXML.Root, diaglang.SelectedLanguage);
                    }

                    XElement catalogXML = catXML.Descendants(XName.Get("Catalog", ns)).ElementAt<XElement>(0);
                    Import.DeviceList = CatalogHelper.GetDevicesFromCatalog(catalogXML);

                    if (Import.DeviceList.Count == 0) continue;

                    if(Import.DeviceList.Count > 1)
                    {
                        foreach(Device device in Import.DeviceList)
                        {
                            SlideListItemBase swipe = new SlideListItemBase();
                            swipe.LeftSymbol = Symbol.Accept;
                            swipe.LeftBackground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 128, 34));
                            device.SlideSettings = swipe;
                        }

                        GridImportDevices.Visibility = Visibility.Visible;
                    } else
                    {
                        SlideListItemBase swipe = new SlideListItemBase();
                        swipe.LeftSymbol = Symbol.Accept;
                        swipe.LeftBackground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 128, 34));
                        swipe.IsSelected = true;
                        Import.DeviceList[0].SlideSettings = swipe;
                        ((Frame)this.Parent).Navigate(typeof(Import), Import);
                    }
                }
            }
        }

        private async void ClickCancel(object sender, RoutedEventArgs e)
        {
            Import.Archive.Dispose();
            GridImportDevices.Visibility = Visibility.Collapsed;
            StorageFile file = await ApplicationData.Current.TemporaryFolder.GetFileAsync("temp.knxprod");
            await file.DeleteAsync();
        }

        private void ListView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if(e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse ||
                e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Pen)
            {
                Device device = (Device)((FrameworkElement)e.OriginalSource).DataContext;
                device.SlideSettings.IsSelected = !device.SlideSettings.IsSelected;

                int count = Import.DeviceList.Where<Device>(d => d.SlideSettings.IsSelected == true).Count();

                if (count == 0)
                    ButtonImportSelected.IsEnabled = false;
                else
                    ButtonImportSelected.IsEnabled = true;
            }
        }

        private void ClickSelectAll(object sender, RoutedEventArgs e)
        {
            foreach(Device device in Import.DeviceList)
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
                return;
            }

            List<ApplicationViewModel> apps = new List<ApplicationViewModel>();
            Hardware2AppModel model = _context.Hardware2App.Where(h => h.HardwareId == device.HardwareId).OrderByDescending(h => h.Version).First();
            DevInfoApp.Text = model.VersionString;
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
        private void RegChanged(object sender, RoutedEventArgs e)
        {
            SearchTextChanged(null, null);
        }

        private void RowDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            ViewHelper.Instance.DragItem = ((DataGridRow)sender).DataContext;
        }

        private void RowLoading(object sender, DataGridRowEventArgs e)
        {
            e.Row.DragStarting += RowDragStarting;
        }
    }
}
