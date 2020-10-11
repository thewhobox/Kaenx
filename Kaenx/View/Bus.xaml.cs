using Kaenx.Classes;
using Kaenx.Classes.Bus;
using Kaenx.Classes.Bus.Actions;
using Kaenx.Classes.Bus.Data;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.Konnect;
using Kaenx.Konnect.Builders;
using Kaenx.Konnect.Connections;
using Kaenx.Konnect.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Timers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class Bus : Page
    {
        public ObservableCollection<IBusData> ReadList { get; } = new ObservableCollection<IBusData>();
        public ObservableCollection<MonitorTelegram> TelegramList { get; } = new ObservableCollection<MonitorTelegram>();

        private Timer _statusTimer = new Timer();
        private IKnxConnection _conn = null;

        public Bus()
        {
            this.InitializeComponent();
            GridReads.DataContext = ReadList;
            GridBusMonitor.DataContext = TelegramList;

            _statusTimer.Interval = TimeSpan.FromSeconds(30).TotalMilliseconds;
            _statusTimer.Elapsed += _statusTimer_Elapsed;
        }

        private void _statusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _conn.SendStatusReq();
        }

        private void ReadInfo(object sender, RoutedEventArgs e)
        {
            DeviceInfo action = new DeviceInfo();

            action.Device = GetDevice();
            if (action.Device == null) return;

            action.Finished += Action_Finished;
            BusConnection.Instance.AddAction(action);
        }

        private void ReadConf(object sender, RoutedEventArgs e)
        {
            DeviceConfig action = new DeviceConfig();

            action.Device = GetDevice();
            if (action.Device == null) return;

            action.Finished += Action_Finished;
            BusConnection.Instance.AddAction(action);
        }

        public void AddReadData(IBusData info)
        {
            _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ReadList.Insert(0, info);
            });
        }

        private void Action_Finished(IBusAction action, object data)
        {
            _ = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                IBusData d = (IBusData)data;
                d.Device = action.Device;
                ReadList.Insert(0, d);
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
                if(BusConnection.Instance.SelectedInterface == null)
                {
                    ViewHelper.Instance.ShowNotification("main", "Bitte wählen Sie erst eine Schnittstelle aus", 3000, ViewHelper.MessageType.Error);
                    return;
                }

                _conn = KnxInterfaceHelper.GetConnection(BusConnection.Instance.SelectedInterface);
                _conn.OnTunnelRequest += _conn_OnTunnelAction;
                _conn.OnTunnelResponse += _conn_OnTunnelAction;
                _conn.Connect();
                MonitorTelegram tel = new MonitorTelegram();
                tel.From = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.To = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.Time = DateTime.Now;
                tel.Type = Konnect.Parser.ApciTypes.Connect;
                TelegramList.Insert(0, tel);
                (BtnMonitorToggle.Content as SymbolIcon).Symbol = Symbol.Pause;
                _statusTimer.Start();
            } else
            {
                _statusTimer.Stop();
                _conn.Disconnect();
                _conn = null;
                MonitorTelegram tel = new MonitorTelegram();
                tel.From = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.To = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.Time = DateTime.Now;
                tel.Type = Konnect.Parser.ApciTypes.Disconnect;
                TelegramList.Insert(0, tel);
                (BtnMonitorToggle.Content as SymbolIcon).Symbol = Symbol.Play;
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
                TelegramList.Insert(0, tel);
            });
        }

        private void GridReads_LoadingRowDetails(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridRowDetailsEventArgs e)
        {
            switch (e.Row.DataContext)
            {
                case DeviceInfoData info:
                    e.Row.DetailsTemplate = Resources["RowDetailsInfoTemplate"] as DataTemplate;
                    break;

                case DeviceConfigData conf:
                    e.Row.DetailsTemplate = Resources["RowDetailsConfigTemplate"] as DataTemplate;
                    break;

                case ErrorData err:
                    e.Row.DetailsTemplate = Resources["RowDetailsErrorTemplate"] as DataTemplate;
                    break;
            }
        }

        private void ClickOpenConfig(object sender, RoutedEventArgs e)
        {
            ViewHelper.Instance.ShowNotification("main", "Nichts passiert");
        }

        private LineDevice GetDevice()
        {
            LineDevice dev = null;


            bool valid = Microsoft.Toolkit.Uwp.UI.Extensions.TextBoxRegex.GetIsValid(InAddress2);

            if (!valid)
            {
                ViewHelper.Instance.ShowNotification("main", "Ungültige Adresse!", 3000, ViewHelper.MessageType.Error);
                return null;
            }

            string[] address = InAddress2.Text.Split(".");

            try
            {
                Line l = SaveHelper._project.Lines.Single(l => l.Id.ToString() == address[0]);
                LineMiddle lm = l.Subs.Single(l => l.Id.ToString() == address[1]);
                LineDevice ld = lm.Subs.Single(l => l.Id.ToString() == address[2]);
                dev = ld;
            }
            catch
            {
                ViewHelper.Instance.ShowNotification("main", "Adresse konnte keinem Gerät zugewiesen werden.", 3000, ViewHelper.MessageType.Warning);
            }

            if (dev == null)
            {
                Line dM = new Line { IsInit = true, Id = int.Parse(address[0]) };
                LineMiddle dL = new LineMiddle { IsInit = true, Id = int.Parse(address[1]), Parent = dM };
                dev = new LineDevice(true) { Name = "Unbekannt", Id = int.Parse(address[2]), Parent = dL };
            }

            return dev;
        }

        private void Monitor_Delete(object sender, RoutedEventArgs e)
        {
            TelegramList.Clear();
        }

        private void ReadMem(object sender, RoutedEventArgs e)
        {
            DeviceMem action = new DeviceMem();

            action.Device = GetDevice();
            if (action.Device == null) return;

            action.Finished += Action_Finished;
            BusConnection.Instance.AddAction(action);
        }
    }
}
