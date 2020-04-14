using Kaenx.Classes;
using Kaenx.Classes.Bus;
using Kaenx.Classes.Bus.Actions;
using Kaenx.Classes.Bus.Data;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View.Controls.Dialogs
{
    public sealed partial class DiagImportDevice : ContentDialog, INotifyPropertyChanged
    {
        public ObservableCollection<DeviceViewModel> DeviceList { get; set; } = new ObservableCollection<DeviceViewModel>();
        public ObservableCollection<LineMiddle> LineList { get; set; } = new ObservableCollection<LineMiddle>();
        public NewDeviceData Device { get; set; }

        private DeviceViewModel _selected;
        public DeviceViewModel SelectedDevice
        {
            get { return _selected; }
            set { _selected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedDevice")); }
        }

        private ResourceLoader loader = ResourceLoader.GetForCurrentView("Topologie");

        public event PropertyChangedEventHandler PropertyChanged;

        public DiagImportDevice(NewDeviceData device)
        {
            this.InitializeComponent();
            Device = device;


            foreach(Line l in SaveHelper._project.Lines)
                foreach (LineMiddle lm in l.Subs)
                    LineList.Add(lm);
            
            this.DataContext = this;

            if (Device.DeviceModels.Count == 1)
                InDevice.IsEnabled = false;

            Delay();
        }

        public async void Delay()
        {
            await Task.Delay(100);
            InDevice.SelectedIndex = 0;

            //SelectedDevice = Device.DeviceModels[0];
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (InSetAddr.IsChecked == true)
            {
                ProgPhysicalAddressSerial action = new ProgPhysicalAddressSerial();
                LineMiddle lm = InLine.SelectedItem as LineMiddle;

                action.Device = AddDeviceToLine(SelectedDevice, lm);

                BusConnection.Instance.AddAction(action);
            }
        }

        private void InLine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InNumber.IsEnabled = true;
            InNumber.Value = 1;
        }

        private string InNumber_PreviewChanged(NumberBox sender, int Value)
        {
            LineMiddle lm = InLine.SelectedItem as LineMiddle;

            if (lm.Subs.Any(d => d.Id == Value))
            {
                IsPrimaryButtonEnabled = false;
                return "Id schon vorhanden in der Linie";
            }

            IsPrimaryButtonEnabled = true;
            return null;
        }

        private LineDevice AddDeviceToLine(DeviceViewModel model, LineMiddle line)
        {
            CatalogContext _context = new CatalogContext();

            LineDevice device = new LineDevice(model, line, true);
            device.DeviceId = model.Id;
            device.Serial = Device.Serial;

            if (model.IsCoupler)
            {
                if (line.Subs.Any(s => s.Id == 0))
                {
                    ViewHelper.Instance.ShowNotification(loader.GetString("ErrMsgCoupler"), 4000, ViewHelper.MessageType.Error);
                    return null;
                }
                device.Id = 0;
            }
            else if (model.IsPowerSupply)
            {
                if (_context.Devices.Any(d => d.IsPowerSupply && line.Subs.Any(l => l.DeviceId == d.Id)))
                {
                    ViewHelper.Instance.ShowNotification(loader.GetString("ErrMsgPowerSupply"), 4000, ViewHelper.MessageType.Error);
                    return null;
                }
                device.Id = -1;
            }
            else
            {
                if (model.HasIndividualAddress)
                {
                    if (line.Subs.Count(s => s.Id != -1) > 255)
                    {
                        ViewHelper.Instance.ShowNotification(loader.GetString("ErrMsgMaxDevices"), 4000, ViewHelper.MessageType.Error);
                        return null;
                    }
                    device.Id = InNumber.ValueOk;
                }
                else
                {
                    device.Id = -1;
                }
            }

            device.ApplicationId = Device.ApplicationId;

            ProjectContext _contextP = SaveHelper.contextProject;

            LineDeviceModel linedevmodel = new LineDeviceModel();
            linedevmodel.Id = device.Id;
            linedevmodel.ParentId = line.UId;
            linedevmodel.Name = device.Name;
            linedevmodel.ApplicationId = device.ApplicationId;
            linedevmodel.DeviceId = device.DeviceId;
            linedevmodel.ProjectId = SaveHelper._project.Id;
            linedevmodel.Serial = Device.Serial;
            _contextP.LineDevices.Add(linedevmodel);
            _contextP.SaveChanges();
            device.UId = linedevmodel.UId;

            line.Subs.Add(device);
            line.Subs.Sort(l => l.Id);
            line.IsExpanded = true;


            if (_context.AppAdditionals.Any(a => a.Id == device.ApplicationId))
            {
                AppAdditional adds = _context.AppAdditionals.Single(a => a.Id == device.ApplicationId);
                device.ComObjects = SaveHelper.ByteArrayToObject<ObservableCollection<DeviceComObject>>(adds.ComsDefault);
                foreach (DeviceComObject com in device.ComObjects)
                    com.DisplayName = com.Name;
            }
            else
            {
                device.ComObjects = new ObservableCollection<DeviceComObject>();
            }

            device.IsInit = false;
            return device;
        }
    }
}
