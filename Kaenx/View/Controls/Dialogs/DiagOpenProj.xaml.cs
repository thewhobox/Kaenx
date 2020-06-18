using Kaenx.Classes.Project;
using Kaenx.DataContext.Local;
using Kaenx.DataContext.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View.Controls.Dialogs
{
    public sealed partial class DiagOpenProj : ContentDialog
    {
        public ObservableCollection<LocalConnectionProject> Connections { get; set; } = new ObservableCollection<LocalConnectionProject>();
        public ObservableCollection<ProjectModel> Projects { get; set; } = new ObservableCollection<ProjectModel>();
        public LocalConnectionProject SelectedConn;
        public ProjectModel SelectedProj;


        LocalContext context = new LocalContext();

        public DiagOpenProj()
        {
            this.InitializeComponent();
            this.DataContext = this;

            foreach (LocalConnectionProject conn in context.ConnsProject)
                Connections.Add(conn);
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SelectedConn = null;
            SelectedProj = null;
        }

        private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await Task.Delay(1);
            LocalConnectionProject conn = (sender as ComboBox).SelectedItem as LocalConnectionProject;
            ProjectContext contextP = new ProjectContext(conn);

            SelectedConn = conn;

            foreach (ProjectModel proj in contextP.Projects)
                Projects.Add(proj);
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsPrimaryButtonEnabled = true;
            SelectedProj = (sender as ListView).SelectedItem as ProjectModel;
        }
    }
}
