using METS.Classes;
using METS.Classes.Controls;
using METS.Classes.Helper;
using METS.Classes.Project;
using METS.Context.Project;
using Microsoft.AppCenter.Crashes;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace METS.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public ObservableCollection<ProjectModel> ProjectList { get; set; } = new ObservableCollection<ProjectModel>();

        private ResourceLoader loader = ResourceLoader.GetForCurrentView("MainPage");
        private ResourceLoader loaderG = ResourceLoader.GetForCurrentView("Global");
        private ResourceLoader loaderD = ResourceLoader.GetForCurrentView("Dialogs");

        private bool _projectSelected = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool ProjectSelected
        {
            get { return _projectSelected; }
            set { _projectSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ProjectSelected")); }
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
            App._dispatcher = Window.Current.Dispatcher;

            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            AppVersion.Text =  string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);






            LoadProjects();
        }

        private async void LoadProjects()
        {
            foreach(ProjectModel model in SaveHelper.GetProjects())
            {
                ProjectList.Add(model);
            }

            bool didCrash = await Crashes.HasCrashedInLastSessionAsync();

            if(didCrash)
            {
                ErrorReport report = await Crashes.GetLastSessionCrashReportAsync();
                Log.Error("App ist in letzter Sitzung abgestürzt!", report.StackTrace);
                Notify.Show(loader.GetString("AppCrashed"));
            }
        }

        private void OpenCatalog(object sender, RoutedEventArgs e)
        {
            Serilog.Log.Debug("Katalog öffnen");
            App.Navigate(typeof(Catalog), "main");
        }

        private async void OpenNewProjekt(object sender, RoutedEventArgs e)
        {
            DiagNewName diag = new DiagNewName();
            diag.Title = loaderD.GetString("NewNameProjectTitle");
            diag.PrimaryButtonText = loaderD.GetString("NewNameProjectPrimary");
            diag.NewName = loaderD.GetString("NewNameProjectName");
            await diag.ShowAsync();
            if (diag.NewName == null) return;


            LoadScreen.IsLoading = true;
            await Task.Delay(1000);


            Project proj = new Project(diag.NewName);


            Line Backbone = new Line(1, loaderG.GetString("Area"));
            Backbone.Subs.Add(new LineMiddle(1, loaderG.GetString("Line") + " 1", Backbone));
            proj.Lines.Add(Backbone);


            proj.Id = SaveHelper.SaveProject(proj).Id;

            StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Projects", CreationCollisionOption.OpenIfExists);
            await folder.CreateFolderAsync(proj.Id.ToString(), CreationCollisionOption.ReplaceExisting);

            ChangeHandler.Instance = new ChangeHandler(proj.Id);

            Serilog.Log.Debug("Projekt wird geöffnet: " + proj.Id + " - " + proj.Name);

            App.AppFrame.Navigate(typeof(WorkdeskEasy), proj);
        }
        
        private void ClickDelete(object sender, RoutedEventArgs e)
        {
            if(LBProjects.SelectedIndex < 0)
            {
                Notify.Show(loader.GetString("MsgSelectProject"), 3000);
                return;
            }

            ProjectModel proj = (ProjectModel)LBProjects.SelectedItem;
            ProjectList.Remove(proj);

            SaveHelper.DeleteProject(proj.Id);
            Notify.Show(loader.GetString("MsgProjectDeleted"), 3000);

            Serilog.Log.Debug("Projekt wurde gelöscht: " + proj.Id + " - " + proj.Name);
        }

        private async void ClickOpen(object sender, RoutedEventArgs e)
        {
            if (LBProjects.SelectedIndex < 0)
            {
                Notify.Show(loader.GetString("MsgSelectProject"), 3000);
                return;
            }

            LoadScreen.IsLoading = true;

            await Task.Delay(200);

            Project project = SaveHelper.LoadProject(((ProjectModel)LBProjects.SelectedItem).Id);
            ChangeHandler.Instance = new ChangeHandler(project.Id);

            Serilog.Log.Debug("Neues Projekt erstellt: " + project.Id + " - " + project.Name);

            App.AppFrame.Navigate(typeof(WorkdeskEasy), project);
        }

        private void wizardP_Click(object sender, RoutedEventArgs e)
        {
            Crashes.GenerateTestCrash();
        }

        private void LBProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ProjectSelected = LBProjects.SelectedItem != null;
        }
    }
}
