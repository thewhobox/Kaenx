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
using Kaenx.Konnect.Messages.Response;
using Kaenx.View.Controls.Dialogs;
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
using Windows.UI.Core;
using Windows.UI.ViewManagement;
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


        public Bus()
        {
            this.InitializeComponent();
            InBtnRemote.DataContext = BusRemoteConnection.Instance;
            BlockStateRemote.DataContext = BusRemoteConnection.Instance;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string && e.Parameter.ToString() == "main")
            {
                this.DataContext = BusConnection.Instance;
                var currentView = SystemNavigationManager.GetForCurrentView();
                currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                currentView.BackRequested += CurrentView_BackRequested;
                ApplicationView.GetForCurrentView().Title = "Bus";
                ViewHelper.Instance.OnShowNotification += Instance_OnShowNotification;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.BackRequested -= CurrentView_BackRequested;
            ViewHelper.Instance.OnShowNotification -= Instance_OnShowNotification;
        }

        private void Instance_OnShowNotification(string view, string text, int duration, ViewHelper.MessageType type)
        {
            if(view == "main")
            {
                Notify.Show(text, duration);
            }
        }

        private void CurrentView_BackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = true;
            App.Navigate(typeof(MainPage));
        }

        private void ClickCancel(object sender, RoutedEventArgs e)
        {
            BusConnection.Instance.CancelCurrent();
        }

        private async void ClickTestInterface(object sender, RoutedEventArgs e)
        {
            if(BusConnection.Instance.SelectedInterface == null)
            {
                ViewHelper.Instance.ShowNotification("main", "Bitte wählen Sie eine Schnittstelle aus.", 3000, ViewHelper.MessageType.Error);
                return;
            }

            BtnTest.IsEnabled = false;
            IKnxConnection conn = KnxInterfaceHelper.GetConnection(BusConnection.Instance.SelectedInterface, BusRemoteConnection.Instance);
            try
            {
                await conn.Connect();
            }
            catch (Exception ex)
            {
                ViewHelper.Instance.ShowNotification("main", "Fehler bei der Verbindung!\r\n" + ex.Message, 3000, ViewHelper.MessageType.Error);
                BtnTest.IsEnabled = true;
                return;
            }

            ViewHelper.Instance.ShowNotification("main", "Schnittstelle ist erreichbar und hat eine Verbindung zum Bus (" + conn.PhysicalAddress.ToString() + ")", 3000, ViewHelper.MessageType.Error);
            await conn.Disconnect();
            BtnTest.IsEnabled = true;
        }

        private void ClickRemoteConnect(object sender, RoutedEventArgs e)
        {
            DiagRemoteConn diag = new DiagRemoteConn();
            _ = diag.ShowAsync();
        }

        private void ClickRemoteIn(object sender, RoutedEventArgs e)
        {
            DiagRemoteIn diag = new DiagRemoteIn();
            _ = diag.ShowAsync();
        }

        private void ClickRemoteOut(object sender, RoutedEventArgs e)
        {
            DiagRemoteOut diag = new DiagRemoteOut();
            _ = diag.ShowAsync();
        }
    }
}
