using Kaenx.Classes;
using Kaenx.Classes.Helper;
using Kaenx.DataContext.Catalog;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
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

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View.Controls
{
    public sealed partial class DiagAddDevice : ContentDialog, INotifyPropertyChanged
    {
        private ObservableCollection<DeviceViewModel> _catalogDevices = new ObservableCollection<DeviceViewModel>();
        public ObservableCollection<DeviceViewModel> CatalogDevices
        {
            get { return _catalogDevices; }
            set { _catalogDevices = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CatalogDevices")); }
        }

        public ObservableCollection<TopologieBase> Lines { get; set; } = new ObservableCollection<TopologieBase>();


        private TopologieBase _selectedLine;
        public TopologieBase SelectedLine
        {
            get { return _selectedLine; }
            set { _selectedLine = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedLine")); }
        }

        public DeviceViewModel SelectedDevice { get; set; }
        public int Count { get; set; } = 1;

        private List<DeviceViewModel> Devices = new List<DeviceViewModel>();
        private CatalogContext _context = new CatalogContext();

        private ResourceLoader loader = ResourceLoader.GetForCurrentView("Dialogs");
        private DataGridColumn previousSortedColumn = null;
        private Brush _oldBorderBrush = null;

        public event PropertyChangedEventHandler PropertyChanged;

        public DiagAddDevice()
        {
            this.InitializeComponent();
            Load();
        }

        private async void Load()
        {
            this.DataContext = this;

            Dictionary<string, string> manus = new Dictionary<string, string>();
            StorageFile file = null;

            try
            {
               file = await ApplicationData.Current.LocalFolder.GetFileAsync("knx_master.xml");
            }
            catch { }

            if (file == null) return;

            XDocument master = XDocument.Load(await file.OpenStreamForReadAsync());

            foreach (XElement ele in master.Descendants(XName.Get("Manufacturer", master.Root.Name.NamespaceName)))
                manus.Add(ele.Attribute("Id").Value, ele.Attribute("Name").Value);

            foreach (DeviceViewModel model in _context.Devices)
            {
                if (manus.ContainsKey(model.ManufacturerId))
                    model.ManufacturerName = manus[model.ManufacturerId];
                else
                    model.ManufacturerName = loader.GetString("AddDeviceNoManu");
                Devices.Add(model);
                CatalogDevices.Add(model);
            }

            foreach(Line line in SaveHelper._project.Lines)
            {
                Lines.Add(line);
                foreach(LineMiddle middle in line.Subs)
                {
                    Lines.Add(middle);
                }
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if(SelectedDevice == null)
                CatalogDeviceList.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Red);
            else
                CatalogDeviceList.BorderBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);


            if (SelectedLine == null)
            {
                _oldBorderBrush = LineList.BorderBrush;
                LineList.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Red);
            }
            else
                if (_oldBorderBrush != null) LineList.BorderBrush = _oldBorderBrush;


            if(SelectedDevice == null || SelectedLine == null)
                Notify.Show(loader.GetString("AddDeviceRequired"), 3000);

            args.Cancel = SelectedDevice == null || SelectedLine == null;
        }

        private void ContentDialog_CancelButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SelectedDevice = null;
            SelectedLine = null;
        }

        private void SearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (previousSortedColumn != null)
                previousSortedColumn.SortDirection = null;

            string query = SearchBox.Text.ToLower();
            CatalogDevices = new ObservableCollection<DeviceViewModel>(from item in Devices where contains(item.Name.ToLower(), query) || contains(item.OrderNumber.ToLower(), query) || (item.VisibleDescription != null && contains(item.VisibleDescription.ToLower(), query)) select item);
        }

        private bool contains(string input, string query)
        {
            if (query.Contains("{") || query.Contains("[") || query.Contains("(") || query.Contains("*"))
            {
                try
                {
                    Regex reg = new Regex(query);
                    SearchBox.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray);
                    return reg.IsMatch(input);
                }
                catch
                {
                    SearchBox.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Red);
                    return false;
                }
            }
            else
            {
                SearchBox.BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray);
                return input.Contains(query);
            }
        }

        private void CatalogDeviceList_Sorting(object sender, DataGridColumnEventArgs e)
        {
            
        }
    }
}
