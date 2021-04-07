using Kaenx.Classes.Bus;
using Kaenx.DataContext.Local;
using Kaenx.Konnect.Connections;
using System;
using System.Collections.Generic;
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

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View.Controls.Dialogs
{
    public sealed partial class DiagRemoteConn : ContentDialog
    {
        public DiagRemoteConn()
        {
            this.InitializeComponent();
            this.DataContext = BusRemoteConnection.Instance.Remote;

            LocalContext context = new LocalContext();
            List<LocalRemote> remotes = context.Remotes.ToList();
            InRemote.ItemsSource = remotes;

            if (remotes.Any(r => r.Host == BusRemoteConnection.Instance.Remote.Hostname && r.Authentification == BusRemoteConnection.Instance.Remote.Authentification))
                InRemote.SelectedItem = remotes.First(r => r.Host == BusRemoteConnection.Instance.Remote.Hostname && r.Authentification == BusRemoteConnection.Instance.Remote.Authentification);

        }

        private void ClickConnect(object sender, RoutedEventArgs e)
        {
            LocalRemote local = (LocalRemote)InRemote.SelectedItem;
            _ = BusRemoteConnection.Instance.Remote.Connect(local.Host, local.Authentification, local.IsSecure, local.Group, local.Code);
        }

        private void ClickDisconnect(object sender, RoutedEventArgs e)
        {
            _ = BusRemoteConnection.Instance.Remote.Disconnect();
        }
    }
}
