using Kaenx.Classes.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
            this.InitializeComponent();
            ContentFrame.Navigated += ContentFrame_Navigated;

            InfoUpdate.DataContext = Classes.Project.UpdateManager.Instance;
            InfoChange.DataContext = Classes.Project.ChangeHandler.Instance;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
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
            _pages.Add("groups", new Groups() { DataContext = CurrentProject });
            _pages.Add("bus", new Bus() { DataContext = Classes.Bus.BusConnection.Instance });
            _pages.Add("settings", new Settings());
            App._pages = _pages;

            InfoInterfaces.DataContext = Classes.Bus.BusConnection.Instance;
            InfoBus.DataContext = Classes.Bus.BusConnection.Instance;

            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void Instance_OnShowNotification(string text, int duration, ViewHelper.MessageType type)
        {
            object style;
            Resources.TryGetValue("NotifyStyle" + type.ToString(), out style);
            Notify.Style = (Style)style;

            if (duration == -1)
                Notify.Show(text);
            else
                Notify.Show(text, duration);
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            NavView.IsBackEnabled = ContentFrame.CanGoBack;
        }

        private void ItemChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            ContentFrame.BackStack.Clear();
            string tag = ((NavigationViewItem)args.SelectedItem).Tag?.ToString();

            if (args.IsSettingsSelected)
                tag = "settings";

            ContentFrame.Content = _pages[tag];
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
