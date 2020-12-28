using Kaenx.Classes;
using Kaenx.Classes.Bus;
using Kaenx.Classes.Helper;
using Kaenx.Konnect.Connections;
using Kaenx.Konnect.Interfaces;
using Kaenx.Konnect.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace Kaenx.View.Controls.Bus
{
    public sealed partial class BMonitor : UserControl
    {
        public ObservableCollection<MonitorTelegram> TelegramList { get; } = new ObservableCollection<MonitorTelegram>();

        private IKnxConnection _conn = null;
        private Timer _statusTimer = new Timer();


        public BMonitor()
        {
            this.InitializeComponent();
            this.DataContext = TelegramList;

            _statusTimer.Interval = TimeSpan.FromSeconds(30).TotalMilliseconds;
            _statusTimer.Elapsed += _statusTimer_Elapsed;
        }

        private void _statusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _conn.SendStatusReq();
        }



        private void Monitor_Delete(object sender, RoutedEventArgs e)
        {
            TelegramList.Clear();
        }

        private void Monitor_Toggle(object sender, RoutedEventArgs e)
        {
            if (_conn == null)
            {
                if (BusConnection.Instance.SelectedInterface == null)
                {
                    ViewHelper.Instance.ShowNotification("main", "Bitte wählen Sie erst eine Schnittstelle aus", 3000, ViewHelper.MessageType.Error);
                    return;
                }

                _conn = KnxInterfaceHelper.GetConnection(BusConnection.Instance.SelectedInterface, BusRemoteConnection.Instance);
                _conn.OnTunnelRequest += _conn_OnTunnelAction;
                _conn.OnTunnelResponse += _conn_OnTunnelAction;
                _conn.Connect();
                MonitorTelegram tel = new MonitorTelegram();
                tel.From = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.To = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.Time = DateTime.Now;
                tel.Type = ApciTypes.Connect;
                TelegramList.Insert(0, tel);
                (BtnMonitorToggle.Content as SymbolIcon).Symbol = Symbol.Pause;
                _statusTimer.Start();
            }
            else
            {
                _statusTimer.Stop();
                _conn.Disconnect();
                _conn = null;
                MonitorTelegram tel = new MonitorTelegram();
                tel.From = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.To = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.Time = DateTime.Now;
                tel.Type = ApciTypes.Disconnect;
                TelegramList.Insert(0, tel);
                (BtnMonitorToggle.Content as SymbolIcon).Symbol = Symbol.Play;
            }
        }

        private void _conn_OnTunnelAction(Konnect.Messages.IMessage response)
        {
            //Todo IMessageResponse Addr source and destination
            MonitorTelegram tel = new MonitorTelegram();
            tel.From = response.SourceAddress;
            tel.To = (Konnect.Addresses.IKnxAddress)response.DestinationAddress;
            tel.Time = DateTime.Now;
            tel.Data = "0x" + BitConverter.ToString(response.Raw).Replace("-", "");
            tel.Type = response.ApciType;
            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                TelegramList.Insert(0, tel);
            });
        }

    }
}
