using Kaenx.Classes;
using Kaenx.Classes.Bus;
using Kaenx.Classes.Helper;
using Kaenx.Konnect.Addresses;
using Kaenx.Konnect.Classes;
using Kaenx.Konnect.Connections;
using Kaenx.Konnect.Interfaces;
using Kaenx.Konnect.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
        private BusCommon _comm = null;


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

        private async void Monitor_Toggle(object sender, RoutedEventArgs e)
        {
            if (_conn == null)
            {
                if (BusConnection.Instance.SelectedInterface == null)
                {
                    ViewHelper.Instance.ShowNotification("main", "Bitte wählen Sie erst eine Schnittstelle aus", 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                    return;
                }

                _conn = await KnxInterfaceHelper.GetConnection(BusConnection.Instance.SelectedInterface, BusRemoteConnection.Instance.Remote, BusConnection.Instance.GetDevice);
                _conn.OnTunnelRequest += _conn_OnTunnelAction;
                _conn.OnTunnelResponse += _conn_OnTunnelAction;
                try
                {
                    await _conn.Connect();
                } catch(Exception ex)
                {
                    ViewHelper.Instance.ShowNotification("main", ex.Message, 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                    return;
                }
                MonitorTelegram tel = new MonitorTelegram();
                tel.From = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.To = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.Time = DateTime.Now;
                tel.Type = ApciTypes.Connect;
                TelegramList.Insert(0, tel);
                (BtnMonitorToggle.Content as SymbolIcon).Symbol = Symbol.Pause;
                _statusTimer.Start();
                _comm = new BusCommon(_conn);
            }
            else
            {
                _statusTimer.Stop();
                await _conn.Disconnect();
                _conn = null;
                _comm = null;
                MonitorTelegram tel = new MonitorTelegram();
                tel.From = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.To = Konnect.Addresses.UnicastAddress.FromString("0.0.0");
                tel.Time = DateTime.Now;
                tel.Type = ApciTypes.Disconnect;
                TelegramList.Insert(0, tel);
                (BtnMonitorToggle.Content as SymbolIcon).Symbol = Symbol.Play;
            }
        }

        private void _conn_OnTunnelAction(IMessage response)
        {
            if(response == null)
            {
                return;
            }

            //Todo IMessageResponse Addr source and destination
            MonitorTelegram tel = new MonitorTelegram();
            tel.From = response.SourceAddress;
            tel.To = response.DestinationAddress;
            tel.Time = DateTime.Now;
            tel.Data = "0x" + BitConverter.ToString(response.Raw).Replace("-", "");
            tel.Type = response.ApciType;
            _ = App._dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                TelegramList.Insert(0, tel);
            });
        }

        private void Monitor_Write(object sender, RoutedEventArgs e)
        {
            MulticastAddress dest = null;
            try
            {
                dest = MulticastAddress.FromString(InDestination.Text);
            }
            catch
            {
                ViewHelper.Instance.ShowNotification("main", "Gruppenadresse ist ungültig!", 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                return;
            }
            _comm.GroupValueWrite(dest, ConvertHexStringToByteArray(InData.Text));
        }

        private async void Monitor_Read(object sender, RoutedEventArgs e)
        {
            MulticastAddress dest = null;
            try
            {
                dest = MulticastAddress.FromString(InDestination.Text);
            }
            catch
            {
                ViewHelper.Instance.ShowNotification("main", "Gruppenadresse ist ungültig!", 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                return;
            }

            Konnect.Messages.Response.IMessageResponse resp = await _comm.GroupValueRead(dest);
        }


        private static byte[] ConvertHexStringToByteArray(string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                hexString = "0" + hexString;
            }

            byte[] data = new byte[hexString.Length / 2];
            for (int index = 0; index < data.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;
        }

    }
}
