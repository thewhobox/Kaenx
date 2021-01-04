using Kaenx.Classes.Bus;
using Kaenx.Classes.Helper;
using Kaenx.DataContext.Local;
using Kaenx.Konnect.Connections;
using Kaenx.Konnect.Interfaces;
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
using Windows.Storage;
using Windows.Storage.Pickers;
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
        public ObservableCollection<LocalConnectionProject> ListDatabases { get; set; } = new ObservableCollection<LocalConnectionProject>();
        public ObservableCollection<LocalRemote> ListRemotes { get; set; } = new ObservableCollection<LocalRemote>();

        public SBusConnections()
        {
            this.InitializeComponent();
            this.DataContext = this;

            foreach(LocalInterface inter in _context.Interfaces)
                ListInterfaces.Add(inter);

            foreach (LocalConnectionProject pro in _context.ConnsProject)
                ListDatabases.Add(pro);

            foreach (LocalRemote rem in _context.Remotes)
                ListRemotes.Add(rem);
        }

        private void ClickToggleAddInterface(object sender, RoutedEventArgs e)
        {
            DiagNewInterface.Visibility = DiagNewInterface.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ClickToggleAddProj(object sender, RoutedEventArgs e)
        {
            InProjPath.Text = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            ToolTipService.SetToolTip(InProjPath, InProjPath.Text);
            DiagNewProjConn.Visibility = DiagNewProjConn.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ClickToggleAddFile(object sender, RoutedEventArgs e)
        {
            DiagNewFileConn.Visibility = DiagNewFileConn.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ClickToggleAddRemote(object sender, RoutedEventArgs e)
        {
            DiagNewRemote.Visibility = DiagNewRemote.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ClickSaveInterface(object sender, RoutedEventArgs e)
        {
            if (!CheckInputs()) return;

            LocalInterface inter = new LocalInterface();
            inter.Ip = InInterAddress.Text;
            inter.Port = int.Parse(InInterPort.Text);
            inter.Name = InInterName.Text;
            inter.Type = InterfaceType.IP;
            _context.Interfaces.Add(inter);
            _context.SaveChanges();

            DiagNewInterface.Visibility = Visibility.Collapsed;

            InInterAddress.Text = "";
            InInterName.Text = "";
            InInterPort.Text = "";

            KnxInterfaceIp binter = new KnxInterfaceIp();
            binter.Name = inter.Name;
            binter.IP = inter.Ip;
            binter.Port = inter.Port;


            BusConnection.Instance.InterfaceList.Add(binter);
            ListInterfaces.Add(inter);
        }

        private bool CheckInputs(bool onlyTest = false)
        {
            if (!onlyTest && string.IsNullOrEmpty(InInterName.Text))
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

            return true;
        }

        private void ClickDeleteConn(object sender, RoutedEventArgs e)
        {
            LocalInterface inter = (sender as Button).DataContext as LocalInterface;
            ListInterfaces.Remove(inter);
            _context.Interfaces.Remove(inter);
            _context.SaveChanges();

            try
            {
                IKnxInterface binter = BusConnection.Instance.InterfaceList.Single(b => b.Hash == inter.Name + "#IP#" + inter.Ip + ":" + inter.Port);
                BusConnection.Instance.InterfaceList.Remove(binter);
            }
            catch { 
            }

            ViewHelper.Instance.ShowNotification("settings", "Schnittstelle erfolgreich gelöscht.", 3000, ViewHelper.MessageType.Success);
        }

        private void ClickDeleteData(object sender, RoutedEventArgs e)
        {
            LocalConnectionProject conn = (sender as Button).DataContext as LocalConnectionProject;
            ListDatabases.Remove(conn);
            _context.ConnsProject.Remove(conn);
            _context.SaveChanges();
        }

        private void ClickDeleteRemote(object sender, RoutedEventArgs e)
        {
            LocalRemote rem = (sender as Button).DataContext as LocalRemote;
            ListRemotes.Remove(rem);
            _context.Remotes.Remove(rem);
            _context.SaveChanges();
        }

        private async void ClickTest(object sender, RoutedEventArgs e)
        {
            if (!CheckInputs(true)) return;

            Konnect.Connections.IKnxConnection conn = new Konnect.Connections.KnxIpTunneling(new IPEndPoint(IPAddress.Parse(InInterAddress.Text), int.Parse(InInterPort.Text)));

            try
            {
                await conn.Connect();
            }
            catch(Exception ex)
            {
                ViewHelper.Instance.ShowNotification("settings", "Fehler bei der Verbindung!\r\n" + ex.Message, 3000, ViewHelper.MessageType.Error);
                return;
            }
            
            ViewHelper.Instance.ShowNotification("settings", "Schnittstelle ist erreichbar und hat eine Verbindung zum Bus (" + conn.PhysicalAddress.ToString() + ")", 3000, ViewHelper.MessageType.Error);
            //await conn.Disconnect();
        }

        private void ClickTest2(object sender, RoutedEventArgs e)
        {

        }

        private void ClickTestRemote(object sender, RoutedEventArgs e)
        {

        }

        private void ClickSaveProjConn(object sender, RoutedEventArgs e)
        {
            LocalConnectionProject conn = new LocalConnectionProject();
            conn.Name = InProjName.Text;
            conn.DbPassword = InProjPass.Text;

            switch (InProjType.SelectedValue)
            {
                case "sqlite":
                    conn.Type = LocalConnectionProject.DbConnectionType.SqlLite;
                    conn.DbHostname = Path.Combine(InProjPath.Text, InProjDbName.Text + ".db");
                    break;

                case "mysql":
                    conn.Type = LocalConnectionProject.DbConnectionType.MySQL;
                    conn.DbHostname = InProjHost.Text;
                    conn.DbUsername = InProjUser.Text;
                    conn.DbName = InProjDbName.Text;
                    break;
            }

            _context.ConnsProject.Add(conn);
            _context.SaveChanges();
            ListDatabases.Add(conn);
            ClickToggleAddProj(null, null);
        }

        private async void ClickChangePath(object sender, RoutedEventArgs e)
        {
            Windows.Storage.Pickers.FolderPicker picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            Windows.Storage.StorageFolder folder = await picker.PickSingleFolderAsync();
            InProjPath.Text = folder.Path;
            Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("myname", folder);
        }

        private async void ClickChangeFile(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".db");
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            StorageFile file = await picker.PickSingleFileAsync();
            if (file == null) return;
            Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.AddOrReplace("ImportDb", file);
        }

        private void InProjType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InProjHost == null) return;


            bool isMySQL = InProjType.SelectedValue.ToString() == "mysql";


            InProjHost.Visibility = isMySQL ? Visibility.Visible : Visibility.Collapsed;
            InProjUser.Visibility = isMySQL ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void ClickSaveFileConn(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(InFileName.Text))
            {
                ViewHelper.Instance.ShowNotification("settings", "Bitte gib einen Namen ein.", 3000, ViewHelper.MessageType.Success);
                return;
            }

            StorageFile file = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync("ImportDb");
            await file.RenameAsync(InFileName.Text + ".db");
            await file.MoveAsync(ApplicationData.Current.LocalFolder);

            LocalConnectionProject conn = new LocalConnectionProject();
            conn.Name = InFileName.Text;
            conn.DbPassword = InFilePass.Text;
            conn.Type = LocalConnectionProject.DbConnectionType.SqlLite;
            conn.DbHostname = InFileName.Text + ".db";

            _context.ConnsProject.Add(conn);
            _context.SaveChanges();
            ListDatabases.Add(conn);
            ClickToggleAddFile(null, null);
        }

        private void ClickSaveRemote(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(InRemoteName.Text))
            {
                ViewHelper.Instance.ShowNotification("settings", "Bitte gib einen Namen ein.", 3000, ViewHelper.MessageType.Success);
                return;
            }
            if (string.IsNullOrEmpty(InRemoteHost.Text))
            {
                ViewHelper.Instance.ShowNotification("settings", "Bitte gib einen Hostnamen ein.", 3000, ViewHelper.MessageType.Success);
                return;
            }
            if (string.IsNullOrEmpty(InRemoteAuth.Text))
            {
                ViewHelper.Instance.ShowNotification("settings", "Bitte gib einen gültigen Authentifizierungskey ein.", 3000, ViewHelper.MessageType.Success);
                return;
            }

            LocalRemote rem = new LocalRemote();
            rem.Name = InRemoteName.Text;
            rem.Authentification = InRemoteAuth.Text;
            rem.Host = InRemoteHost.Text;
            rem.Group = InRemoteGroup.Text;
            rem.Code = InRemoteCode.Text;
            rem.IsSecure = InRemoteSecure.IsChecked == true;

            _context.Remotes.Add(rem);
            _context.SaveChanges();
            ListRemotes.Add(rem);
            ClickToggleAddRemote(null, null);
        }
    }
}
