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
using Kaenx.DataContext.Import;
using Kaenx.DataContext.Import.Manager;
using Kaenx.Classes.Catalog;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Kaenx.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Catalog : Page, INotifyPropertyChanged
    {
        private bool wasFromMain = false;
        private int lastCategorie = -1;
        private ImportTypes lastType = ImportTypes.Undefined;
        private ResourceLoader loader = ResourceLoader.GetForCurrentView("Catalog");
        private ObservableCollection<DeviceViewModel> _items = new ObservableCollection<DeviceViewModel>();
        private ObservableCollection<DeviceViewModel> _catalogDevices = new ObservableCollection<DeviceViewModel>();
        public ObservableCollection<DeviceViewModel> CatalogDevices
        {
            get { return _catalogDevices; }
            set { _catalogDevices = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CatalogDevices")); }
        }

        public ObservableCollection<DeviceListItem> DeviceList { get; set; } = new ObservableCollection<DeviceListItem>();

        private CatalogContext _context = new CatalogContext();

        public event PropertyChangedEventHandler PropertyChanged;

        public Catalog()
        {
            this.InitializeComponent();

            var mainNode = new Classes.TVNode();
            mainNode.Content = loader.GetString("MansListAll");
            mainNode.SectionId = -2;
            mainNode.IsExpanded = true;
            TreeV.RootNodes.Add(mainNode);


            var subNode = new TVNode()
            {
                Content = "ETS",
                SectionId = -1,
                ImportType = ImportTypes.ETS,
                IsExpanded = true
            };
            mainNode.Children.Add(subNode);
            subNode = new TVNode()
            {
                Content = "Konnekting",
                SectionId = -1,
                ImportType = ImportTypes.Konnekting,
                IsExpanded = true
            };
            mainNode.Children.Add(subNode);

            LoadDevices(-1, ImportTypes.ETS);
            LoadDevices(-1, ImportTypes.Konnekting);
            LoadSections(mainNode);

            this.DataContext = this;


        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if(e.Parameter is StorageFile)
            {
                //PrepareImport(e.Parameter as StorageFile);
                wasFromMain = true;
                ApplicationView.GetForCurrentView().Title = loader.GetString("WindowTitle");

                var currentView = SystemNavigationManager.GetForCurrentView();
                currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                currentView.BackRequested += CurrentView_BackRequested;
            } else if(e.Parameter is string && e.Parameter.ToString() == "main") 
            {
                wasFromMain = true;
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
            Frame main = this.Parent as Frame;
            main.Navigate(typeof(Import), wasFromMain);
        }

        private void TreeV_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            Classes.TVNode node = (Classes.TVNode)args.InvokedItem;
            LoadDevices(node.SectionId, node.ImportType);
        }

        private async void LoadDevices(int section, ImportTypes type)
        {
            lastCategorie = section;
            CatalogDevices.Clear();
            _items.Clear();
            List<int> cats = new List<int>();
            cats.Add(section);
            await Task.Delay(290);

            if (section == -2)
            {
                GetSubSection(cats, -1, ImportTypes.ETS);
                GetSubSection(cats, -1, ImportTypes.Konnekting);
            } else
            {
                GetSubSection(cats, section, type);
            }


            foreach(DeviceViewModel model in _context.Devices.Where(dev => cats.Contains(dev.CatalogId)).ToList().OrderBy(dev => dev.Name))
            {
                CatalogDevices.Add(model);
                _items.Add(model);
            }
        }

        private void GetSubSection(List<int> list, int section, ImportTypes type)
        {
            IEnumerable<CatalogViewModel> sections = _context.Sections.Where(sec => sec.ImportType == type && sec.ParentId == section);
            foreach (CatalogViewModel cat in sections)
            {
                if (!list.Contains(cat.Id)) list.Add(cat.Id);
                GetSubSection(list, cat.Id, type);
            }
        }

        private void LoadSections(TVNode node)
        {
            IEnumerable<CatalogViewModel> sections = _context.Sections.Where(sec => sec.ParentId == node.SectionId);

            foreach(CatalogViewModel sec in sections)
            {
                var secNode = new Classes.TVNode();
                secNode.Content = sec.Name;
                secNode.SectionId = sec.Id;
                node.Children.Add(secNode);
                LoadSections(secNode);
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
            IEnumerable<Hardware2AppModel> models = _context.Hardware2App.Where(h => h.Id == device.HardwareId).OrderByDescending(h => h.Version);

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

            //TODO richtig machen!
            foreach(DeviceViewModel device in CatalogDeviceList.SelectedItems)
            {
                _context.Devices.Remove(device);

                if (device.HasApplicationProgram)
                {
                    int count = _context.Devices.Count(d => d != device && d.HardwareId == device.HardwareId);
                    if (count == 0)
                    {
                        Hardware2AppModel h2a = _context.Hardware2App.Single(h => h.Id == device.HardwareId);
                        _context.Hardware2App.Remove(h2a);

                        foreach(ApplicationViewModel app in _context.Applications.Where(a => a.HardwareId == h2a.Id).ToList())
                        {
                            IEnumerable<object> tempList = _context.AppSegments.Where(a => a.ApplicationId == app.Id);
                            _context.RemoveRange(tempList);

                            tempList = _context.AppComObjects.Where(a => a.ApplicationId == app.Id);
                            _context.RemoveRange(tempList);

                            tempList = _context.AppAdditionals.Where(a => a.ApplicationId == app.Id);
                            _context.RemoveRange(tempList);

                            tempList = _context.AppParameters.Where(a => a.ApplicationId == app.Id);
                            _context.RemoveRange(tempList);

                            //Check database so everything deletes

                            List<AppParameterTypeViewModel> toDelete = new List<AppParameterTypeViewModel>();
                            tempList = _context.AppParameterTypes.Where(p => p.ApplicationId == app.Id);
                            foreach (AppParameterTypeViewModel pType in tempList)
                            {
                                if (pType.Type == ParamTypes.Enum)
                                {
                                    IEnumerable<AppParameterTypeEnumViewModel> tempList2 = _context.AppParameterTypeEnums.Where(e => e.TypeId == pType.Id);
                                    _context.AppParameterTypeEnums.RemoveRange(tempList2);
                                }
                                toDelete.Add(pType);
                            }
                            _context.AppParameterTypes.RemoveRange(toDelete);

                            

                            _context.Applications.Remove(app);
                        }
                    }
                }
            }

            _context.SaveChanges();
            if(lastType == ImportTypes.Undefined)
            {
                LoadDevices(lastCategorie, ImportTypes.ETS);
                LoadDevices(lastCategorie, ImportTypes.Konnekting);
            } else
            {
                LoadDevices(lastCategorie, lastType);
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