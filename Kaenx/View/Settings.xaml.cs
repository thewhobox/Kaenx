﻿using Kaenx.Classes.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
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

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class Settings : Page
    {
        private ResourceLoader loader = ResourceLoader.GetForCurrentView("Settings");

        public Settings()
        {
            this.InitializeComponent();

            this.Loaded += (a,b) =>
            {
                ViewHelper.Instance.OnShowNotification += Instance_OnShowNotification;
            };
            this.Unloaded += (a, b) =>
            {
                ViewHelper.Instance.OnShowNotification -= Instance_OnShowNotification;
            };
        }

        

        private void Instance_OnShowNotification(string view, string text, int duration, ViewHelper.MessageType type)
        {
            if (view != "all" && view != "settings") return;
            try
            {
                object style;
                Resources.TryGetValue("NotifyStyle" + type.ToString(), out style);
                if(style != null)
                    Notify.Style = (Style)style;

                if (duration == -1)
                    Notify.Show(text);
                else
                    Notify.Show(text, duration);
            }
            catch { }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string && e.Parameter.ToString() == "main")
            {
                var currentView = SystemNavigationManager.GetForCurrentView();
                currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                currentView.BackRequested += CurrentView_BackRequested;
                ApplicationView.GetForCurrentView().Title = loader.GetString("WindowTitle");
            }
        }

        private void CurrentView_BackRequested(object sender, BackRequestedEventArgs e)
        {
            App.Navigate(typeof(MainPage));
        }
    }
}
