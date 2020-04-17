using Kaenx.Classes.Bus;
using Kaenx.Classes.Helper;
using Kaenx.DataContext.Local;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

namespace Kaenx.View.Controls.Settings
{
    public sealed partial class SBusConnections : UserControl
    {
        private LocalContext _context = new LocalContext();
        public ObservableCollection<LocalInterface> ListInterfaces { get; set; } = new ObservableCollection<LocalInterface>();

        public SBusConnections()
        {
            this.InitializeComponent();
            this.DataContext = this;


            foreach(LocalInterface inter in _context.Interfaces)
            {
                ListInterfaces.Add(inter);
            }
        }

        private void ClickToggleAddInterface(object sender, RoutedEventArgs e)
        {
            DiagNewInterface.Visibility = DiagNewInterface.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ClickSaveInterface(object sender, RoutedEventArgs e)
        {
            if (!CheckInputs()) return;


            LocalInterface inter = new LocalInterface();
            inter.Ip = InInterAddress.Text;
            inter.Port = int.Parse(InInterPort.Text);
            inter.Name = InInterName.Text;
            inter.PhAddr = InInterPhAddr.Text;

            _context.Interfaces.Add(inter);
            _context.SaveChanges();

            DiagNewInterface.Visibility = Visibility.Collapsed;

            InInterAddress.Text = "";
            InInterName.Text = "";
            InInterPort.Text = "";
            InInterPhAddr.Text = "";

            BusInterface binter = new BusInterface();
            binter.Name = inter.Name;
            binter.Endpoint = new IPEndPoint(IPAddress.Parse(inter.Ip), inter.Port);
            binter.Hash = inter.Id.ToString();
            BusConnection.Instance.InterfaceList.Add(binter);
            ListInterfaces.Add(inter);
        }

        private bool CheckInputs()
        {
            if (string.IsNullOrEmpty(InInterName.Text))
            {
                ViewHelper.Instance.ShowNotification("settings", "Bitte gib einen Namen ein.", 3000, ViewHelper.MessageType.Error);
                return false;
            }

            string[] ip = InInterAddress.Text.Split(".");
            if (ip.Count() != 4)
            {
                ViewHelper.Instance.ShowNotification("settings", "Bitte gib eine gültige IP Adresse ein.", 3000, ViewHelper.MessageType.Error);
                return false;
            }
            else
            {
                bool fail = false;
                for (int i = 0; i < 4; i++)
                {
                    int integerOut;
                    if (!int.TryParse(ip[i], out integerOut) || integerOut > 255) fail = true;
                }

                if (fail)
                {
                    ViewHelper.Instance.ShowNotification("settings", "Bitte gib eine gültige IP Adresse ein.", 3000, ViewHelper.MessageType.Error);
                    return false;
                }
            }

            int port;
            if (!int.TryParse(InInterPort.Text, out port))
            {
                ViewHelper.Instance.ShowNotification("settings", "Bitte gib einen gültige Port ein.", 3000, ViewHelper.MessageType.Error);
                return false;
            }

            string[] addr = InInterPhAddr.Text.Split(".");
            if (addr.Count() != 3)
            {
                ViewHelper.Instance.ShowNotification("settings", "Bitte gib eine gültige Physikalische Adresse ein.", 3000, ViewHelper.MessageType.Error);
                return false;
            }
            else
            {
                bool fail = false;
                int main, line, device;
                if (!int.TryParse(addr[0], out main) || main > 15) fail = true;
                if (!int.TryParse(addr[1], out line) || line > 15) fail = true;
                if (!int.TryParse(addr[2], out device) || device > 255) fail = true;

                if (fail)
                {
                    ViewHelper.Instance.ShowNotification("settings", "Bitte gib eine gültige Physikalische Adresse ein.", 3000, ViewHelper.MessageType.Error);
                    return false;
                }
            }

            return true;
        }

        private void ClickDelete(object sender, RoutedEventArgs e)
        {
            LocalInterface inter = (sender as Button).DataContext as LocalInterface;
            ListInterfaces.Remove(inter);
            _context.Interfaces.Remove(inter);
            _context.SaveChanges();

            try
            {
                BusInterface binter = BusConnection.Instance.InterfaceList.Single(b => b.Hash == inter.Id.ToString());
                BusConnection.Instance.InterfaceList.Remove(binter);
            }
            catch { }

            ViewHelper.Instance.ShowNotification("settings", "Schnittstelle erfolgreich gelöscht.", 3000, ViewHelper.MessageType.Success);
        }

        private async void ClickTest(object sender, RoutedEventArgs e)
        {
            if (!CheckInputs()) return;

            Konnect.Connection conn = new Konnect.Connection(new IPEndPoint(IPAddress.Parse(InInterAddress.Text), int.Parse(InInterPort.Text)));
            conn.Connect();
            await Task.Delay(1000);
            if(conn.IsConnected)
                ViewHelper.Instance.ShowNotification("settings", "Schnittstelle ist erreichbar.", 3000, ViewHelper.MessageType.Success);
            else
                ViewHelper.Instance.ShowNotification("settings", "Schnittstelle nicht erreichbar.", 3000, ViewHelper.MessageType.Error);
            conn.Disconnect();
        }
    }
}
