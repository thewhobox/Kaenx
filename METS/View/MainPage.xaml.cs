using METS.Classes;
using METS.Classes.Controls;
using METS.Classes.Helper;
using METS.Classes.Project;
using METS.Context.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Serialization;
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
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<ProjectModel> ProjectList { get; set; } = new ObservableCollection<ProjectModel>();

        private ResourceLoader loader = ResourceLoader.GetForCurrentView("MainPage");
        private ResourceLoader loaderG = ResourceLoader.GetForCurrentView("Global");

        public MainPage()
        {
            this.InitializeComponent();
            LBProjects.DataContext = ProjectList;
            App._dispatcher = Window.Current.Dispatcher;

            LoadProjects();
            //test.Source = new Windows.UI.Xaml.Media.Imaging.SvgImageSource(new Uri("ms-appx:///Data/Logos/full.svg", UriKind.Absolute));
            //test.ImageFailed += Test_ImageFailed;
        }

        private void Test_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void LoadProjects()
        {
            foreach(ProjectModel model in SaveHelper.GetProjects())
            {
                ProjectList.Add(model);
            }
        }

        private void OpenCatalog(object sender, RoutedEventArgs e)
        {
            App.Navigate(typeof(Catalog), typeof(MainPage));
        }

        private async void OpenNewProjekt(object sender, RoutedEventArgs e)
        {
            DiagNewName diag = new DiagNewName();
            diag.Title = loader.GetString("DiagNewTitle");
            diag.PrimaryButtonText = loader.GetString("DiagNewPrimary");
            diag.NewName = loader.GetString("DiagNewName");
            await diag.ShowAsync();
            if (diag.NewName == null) return;


            LoadScreen.IsLoading = true;
            await Task.Delay(1000);


            Project proj = new Project(diag.NewName);


            Line Backbone = new Line(1, loaderG.GetString("Area"));
            Backbone.Subs.Add(new LineMiddle(1, loaderG.GetString("Line") + " 1", Backbone));
            proj.Lines.Add(Backbone);


            proj.Id = SaveHelper.SaveProject(proj).Id;

            ChangeHandler.Instance = new ChangeHandler(proj.Id);

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

            App.AppFrame.Navigate(typeof(WorkdeskEasy), project);
        }
    }
}
