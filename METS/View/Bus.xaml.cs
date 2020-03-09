using Kaenx.Classes;
using Kaenx.Classes.Bus;
using Kaenx.Classes.Bus.Actions;
using Kaenx.Classes.Helper;
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

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class Bus : Page
    {
        public ObservableCollection<DeviceInfoData> ReadList { get; set; } = new ObservableCollection<DeviceInfoData>();

        public Bus()
        {
            this.InitializeComponent();
            GridReads.DataContext = ReadList;
        }

        private void ReadInfo(object sender, RoutedEventArgs e)
        {
            string[] address = InAddress2.Text.Split(".");

            if(address.Length != 3)
            {
                ViewHelper.Instance.ShowNotification("Ungültige Adresse!", 3000, ViewHelper.MessageType.Error);
                return;
            }

            DeviceInfo action = new DeviceInfo();
            Line dM = new Line { Id = int.Parse(address[0]) };
            LineMiddle dL = new LineMiddle { Id = int.Parse(address[1]), Parent = dM };
            action.Device = new LineDevice { Name = "Unbekannt", Id = int.Parse(address[2]), Parent = dL };
            action.Finished += Action_Finished;
            BusConnection.Instance.AddAction(action);
        
        }

        private void Action_Finished(IBusAction action, object data)
        {
            _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                DeviceInfoData d = (DeviceInfoData)data;
                d.Device = action.Device;
                ReadList.Add(d);
            });
        }

        private void ClickCancel(object sender, RoutedEventArgs e)
        {
            BusConnection.Instance.CancelCurrent();
        }
    }
}
