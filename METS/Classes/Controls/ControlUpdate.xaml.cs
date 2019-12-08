using METS.Classes.Project;
using METS.Context.Catalog;
using METS.Context.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace METS.Classes.Controls
{
    public sealed partial class ControlUpdate : UserControl
    {
        private CatalogContext _context = new CatalogContext();
        private ProjectContext _contextP = new ProjectContext();

        public ObservableCollection<DeviceUpdate> Devices { get; set; } = new ObservableCollection<DeviceUpdate>();


        public ControlUpdate()
        {
            this.InitializeComponent();
            this.DataContext = this;
            Devices.CollectionChanged += Devices_CollectionChanged;

            LoadDevices();
        }

        private void Devices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            BtnUpdateAll.IsEnabled = Devices.Count > 0;
        }

        private void LoadDevices()
        {
            Devices.Clear();
            foreach (LineDevice device in UpdateManager.Instance.GetDevices())
            {
                Hardware2AppModel model = _context.Hardware2App.Single(h => h.ApplicationId == device.ApplicationId);
                Hardware2AppModel latestModel = _context.Hardware2App.Where(h => h.HardwareId == model.HardwareId && h.Number == model.Number).OrderByDescending(h => h.Version).First();

                DeviceUpdate update = new DeviceUpdate() { Name = device.LineName + " " + device.Name };
                update.VersionCurrent = model.VersionString;
                update.VersionNew = latestModel.VersionString;
                update.DeviceUid = device.UId;
                update.Device = device;
                update.NewApplicationId = latestModel.ApplicationId;
                Devices.Add(update);
            }
        }

        private void UpdateList(object sender, RoutedEventArgs e)
        {
            LoadDevices();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnUpdate.IsEnabled = ((ListView)sender).SelectedItems.Count > 0;
        }

        private void UpdateDevice(object sender, RoutedEventArgs e)
        {
            foreach(DeviceUpdate update in LVdevices.SelectedItems)
                UpdateDevice(update);
        }

        private void UpdateDeviceAll(object sender, RoutedEventArgs e)
        {
            foreach (DeviceUpdate update in Devices.ToList())
                UpdateDevice(update);
        }

        private void UpdateDevice(DeviceUpdate update)
        {
            Devices.Remove(update);
            LineDeviceModel device = _contextP.LineDevices.Single(d => d.UId == update.DeviceUid);
            device.ApplicationId = update.NewApplicationId;
            _contextP.LineDevices.Update(device);
            _contextP.SaveChanges();
            update.Device.ApplicationId = update.NewApplicationId;
            UpdateManager.Instance.CountUpdates();
        }


    }
}
