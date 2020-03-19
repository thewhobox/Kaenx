using Kaenx.Classes;
using Kaenx.Classes.Bus;
using Kaenx.Classes.Bus.Actions;
using Kaenx.Classes.Helper;
using Kaenx.Konnect;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
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
        public ObservableCollection<DeviceInfoData> ReadList { get; } = new ObservableCollection<DeviceInfoData>();
        public ObservableCollection<MonitorTelegram> TelegramList { get; } = new ObservableCollection<MonitorTelegram>();


        private Connection _conn = null;

        public Bus()
        {
            this.InitializeComponent();
            GridReads.DataContext = ReadList;
            GridBusMonitor.DataContext = TelegramList;
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
            action.Device = new LineDevice(true) { Name = "Unbekannt", Id = int.Parse(address[2]), Parent = dL };
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

        private void Monitor_Toggle(object sender, RoutedEventArgs e)
        {
            if(_conn == null)
            {
                _conn = new Connection(new IPEndPoint(IPAddress.Parse("192.168.0.108"), Convert.ToInt32(3671)));
                _conn.OnTunnelRequest += _conn_OnTunnelAction;
                _conn.OnTunnelResponse += _conn_OnTunnelAction;
                _conn.Connect();
                MonitorTelegram tel = new MonitorTelegram();
                tel.From = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.To = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.Time = DateTime.Now;
                tel.Type = Konnect.Parser.ApciTypes.Connect;
                TelegramList.Add(tel);
            } else
            {
                _conn.Disconnect();
                _conn = null;
                MonitorTelegram tel = new MonitorTelegram();
                tel.From = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.To = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.Time = DateTime.Now;
                tel.Type = Konnect.Parser.ApciTypes.Disconnect;
                TelegramList.Add(tel);
            }
        }

        private void _conn_OnTunnelAction(Konnect.Builders.TunnelResponse response)
        {
            MonitorTelegram tel = new MonitorTelegram();
            tel.From = response.SourceAddress;
            tel.To = (Konnect.Addresses.IKnxAddress)response.DestinationAddress;
            tel.Time = DateTime.Now;
            tel.Data = "0x" + BitConverter.ToString(response.Data).Replace("-", "");
            tel.Type = response.APCI;
            _=App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                TelegramList.Add(tel);
            });
        }
    }
}
