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
            this.DataContext = RemoteConnection.Instance;

            LocalContext context = new LocalContext();
            List<LocalRemote> remotes = context.Remotes.ToList();
            InRemote.ItemsSource = remotes;

            if (RemoteConnection.Instance.ConnectionOut != null && remotes.Any(r => r.Host == RemoteConnection.Instance.ConnectionOut.Hostname && r.Authentification == RemoteConnection.Instance.ConnectionOut.Authentication))
                InRemote.SelectedItem = remotes.First(r => r.Host == RemoteConnection.Instance.ConnectionOut.Hostname && r.Authentification == RemoteConnection.Instance.ConnectionOut.Authentication);

        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ClickConnect(object sender, RoutedEventArgs e)
        {
            _= RemoteConnection.Instance.SetNewConnection((LocalRemote)InRemote.SelectedItem);
        }

        private void ClickDisconnect(object sender, RoutedEventArgs e)
        {
            _=RemoteConnection.Instance.Disconnect();
        }
    }
}
