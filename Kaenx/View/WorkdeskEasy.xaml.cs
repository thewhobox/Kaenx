using Kaenx.Classes.Helper;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class WorkdeskEasy : Page, INotifyPropertyChanged
    {
        private ResourceLoader loader = ResourceLoader.GetForCurrentView("WorkDeskEasy");
        private Kaenx.Classes.Project.Project _project;

        public event PropertyChangedEventHandler PropertyChanged;

        private Kaenx.Classes.Project.Project CurrentProject
        {
            get
            {
                return _project;
            }
            set
            {
                _project = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Project"));
            }
        }

        private Dictionary<string, Page> _pages { get; set; } = new Dictionary<string, Page>();

        public WorkdeskEasy()
        {
            Log.Information("WorkdeskEasy wird initialisiert");
            this.InitializeComponent();
            ContentFrame.Navigated += ContentFrame_Navigated;

            ViewBar.DataContext = Classes.Bus.BusRemoteConnection.Instance.Remote;
            InfoUpdate.DataContext = Classes.Project.UpdateManager.Instance;
            InfoChange.DataContext = Classes.Project.ChangeHandler.Instance;
            Log.Information("WorkdeskEasy initialisierung abgeschlossen");
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Log.Information("WorkdeskEasy OnNavigatedTo");
            try
            {
                base.OnNavigatedTo(e);
                CurrentProject = (Kaenx.Classes.Project.Project)e.Parameter;
                ApplicationView.GetForCurrentView().Title = CurrentProject.Name;

                ViewHelper.Instance.OnShowNotification += Instance_OnShowNotification;

                Classes.Project.UpdateManager.Instance.SetProject(CurrentProject);
                Classes.Project.UpdateManager.Instance.CountUpdates();

                _pages.Add("home", new Home() { DataContext = CurrentProject });
                _pages.Add("catalog", new Catalog());
                _pages.Add("topologie", new Topologie() { DataContext = CurrentProject.Lines, _project = CurrentProject });
                try
                {
                    _pages.Add("groups", new Groups() { DataContext = CurrentProject });
                }catch(Exception ex)
                {
                    Log.Information(ex.Message);
                    Log.Information(ex.StackTrace);
                    if(ex.InnerException != null)
                        Log.Error(ex.InnerException, "Inner Exception");
                }
                _pages.Add("bus", new Bus() { DataContext = Classes.Bus.BusConnection.Instance });
                Log.Information("WorkdeskEasy Init bus");
                _pages.Add("settings", new Settings());
                Log.Information("WorkdeskEasy Init settings");
                App._pages = _pages;

                //InfoInterfaces.DataContext = Classes.Bus.BusConnection.Instance;
                InfoBus.DataContext = Classes.Bus.BusConnection.Instance;
                Log.Information("WorkdeskEasy DataContext");

                Log.Information("WorkdeskEasy " + NavView.MenuItems.Count);
                NavView.SelectedItem = NavView.MenuItems[0];
                Log.Information("WorkdeskEasy Set Startpage");

                var currentView = SystemNavigationManager.GetForCurrentView();
                currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                currentView.BackRequested += CurrentView_BackRequested;
                Log.Information("WorkdeskEasy Set BackButton");
            } catch(Exception ex)
            {
                Log.Error(ex, "Fehler bei Onnavigated to");
                throw ex;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.BackRequested -= CurrentView_BackRequested;
        }

        private void CurrentView_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if(e.Handled) return;
            e.Handled = true;

            if(ContentFrame.Content.GetType() == typeof(Import))
            {
                ContentFrame.Content = _pages["catalog"];
            } else
            {
                App.Navigate(typeof(MainPage));
            }
        }

        private void Instance_OnShowNotification(string view, string text, int duration, InfoBarSeverity type)
        {
            if (view != "all" && view != "main") return;

            _ = App._dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
              {
                  InfoBar info = new InfoBar();
                  info.Message = text;
                  info.Severity = type;

                  switch (type)
                  {
                      case InfoBarSeverity.Warning:
                          info.Title = "Warnung";
                          break;
                      case InfoBarSeverity.Error:
                          info.Title = "Fehler";
                          break;
                      case InfoBarSeverity.Success:
                          info.Title = "Erfolgreich";
                          break;
                      default:
                          info.Title = "Info";
                          break;
                  }

                  info.IsOpen = true;
                  info.Closed += (a, b) => InfoPanel.Children.Remove(a);
                  InfoPanel.Children.Add(info);
              });
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            NavView.IsBackEnabled = ContentFrame.CanGoBack;
        }

        private void ItemChanged(Windows.UI.Xaml.Controls.NavigationView sender, Windows.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            try
            {
                ContentFrame.BackStack.Clear();
                string tag = ((Windows.UI.Xaml.Controls.NavigationViewItem)args.SelectedItem).Tag?.ToString();

                if (args.IsSettingsSelected)
                    tag = "settings";

                ContentFrame.Content = _pages[tag];
            } catch(Exception e)
            {
                Log.Error(e, "Fehler beim SelectionChanged");
                throw e;
            }
        }

        private void InfoBus_Click(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = NavView.MenuItems[5];
        }

        private void ClickCancelAction(object sender, RoutedEventArgs e)
        {
            Classes.Bus.BusConnection.Instance.CancelCurrent();
        }
    }
}
