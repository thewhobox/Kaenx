using Kaenx.Classes;
using Kaenx.Classes.Controls;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Local;
using Kaenx.DataContext.Project;
using Kaenx.Konnect;
using Kaenx.Konnect.Builders;
using Kaenx.View.Controls;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Services.Store;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Kaenx.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public ObservableCollection<ProjectViewHelper> ProjectList { get; set; } = new ObservableCollection<ProjectViewHelper>();
        public ObservableCollection<LocalConnectionProject> ConnectionsList { get; set; } = new ObservableCollection<LocalConnectionProject>();

        private ResourceLoader loader = ResourceLoader.GetForCurrentView("MainPage");
        private ResourceLoader loaderG = ResourceLoader.GetForCurrentView("Global");
        private ResourceLoader loaderD = ResourceLoader.GetForCurrentView("Dialogs");

        private bool _projectSelected = false;
        private LocalContext _contextL = new LocalContext();
        private FlyoutBase _currentFlyout = null;

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
            LocalContext context = new LocalContext();
            foreach(LocalProject model in context.Projects.ToList())
            {
                ProjectViewHelper helper = new ProjectViewHelper();
                helper.Id = model.Id;
                helper.Name = model.Name;
                helper.Local = model;
                helper.IsReconstruct = model.IsReconstruct;
                helper.ProjectId = model.ProjectId;

                if(model.Thumbnail != null)
                {
                    var wb = new WriteableBitmap(512,512);
                    using (Stream stream = wb.PixelBuffer.AsStream())
                    {
                        await stream.WriteAsync(model.Thumbnail, 0, model.Thumbnail.Length);
                    }

                    helper.Image = wb;
                } else
                {
                    helper.Image = new BitmapImage() { UriSource = new Uri("ms-appx:///Assets/FileLogo.png") };
                }

                ProjectList.Add(helper);
            }

            bool didCrash = await Crashes.HasCrashedInLastSessionAsync();

            if(didCrash)
            {
                ErrorReport report = await Crashes.GetLastSessionCrashReportAsync();
                Log.Error("App ist in letzter Sitzung abgestürzt!" + Environment.NewLine + report.StackTrace.Substring(0, report.StackTrace.IndexOf(Environment.NewLine)));
                Notify.Show(loader.GetString("AppCrashed") + Environment.NewLine + report.StackTrace.Substring(0, report.StackTrace.IndexOf(Environment.NewLine)));
            }
        }

        private void OpenCatalog(object sender, RoutedEventArgs e)
        {
            Serilog.Log.Information("Katalog öffnen");
            App.Navigate(typeof(Catalog), "main");
        }

        private void OpenSettings(object sender, RoutedEventArgs e)
        {
            Serilog.Log.Information("Einstellungen öffnen");
            App.Navigate(typeof(Settings), "main");
        }

        private void OpenNewProjekt(object sender, RoutedEventArgs e)
        {
            ConnectionsList.Clear();
            foreach (LocalConnectionProject conn in _contextL.ConnsProject)
                ConnectionsList.Add(conn);

            InConn.SelectedIndex = 0;
            DiagNew.Visibility = Visibility;
            InName.SelectAll();
            InName.Focus(FocusState.Programmatic);
        }


        private void OpenProject(object sender, RoutedEventArgs e)
        {
            ProjectViewHelper helper = (ProjectViewHelper)ProjectsGrid.SelectedItem;
            DoOpenProject(helper);
        }

        private async void DoOpenProject(ProjectViewHelper helper)
        {
            _currentFlyout?.Hide();
            LoadScreen.IsLoading = true;
            await Task.Delay(200);

            Project project;
            try
            {
                project = SaveHelper.LoadProject(helper);
            }
            catch(Exception ex)
            {
                Notify.Show("Das Projekt konnte nicht geladen werden:" + Environment.NewLine + ex.Message, 4000);
                Log.Error(ex.Message, "Verbindung fehlgeschlagen!");
                LoadScreen.IsLoading = false;
                return;
            }


            project.Local = helper.Local;
            if (project == null)
            {
                LoadScreen.IsLoading = true;
                return;
            }

            ChangeHandler.Instance = new ChangeHandler(project.Id);
            Serilog.Log.Information("Projekt geöffnet: " + project.Id + " - " + project.Name);

            if (JumpList.IsSupported())
            {
                JumpList jumpList = await JumpList.LoadCurrentAsync();
                //jumpList.Items.Clear();
                jumpList.SystemGroupKind = JumpListSystemGroupKind.None;


                if (jumpList.Items.Any(i => i.Arguments == "open:" + helper.Id))
                {
                    JumpListItem itemN = jumpList.Items.Single(i => i.Arguments == "open:" + helper.Id);
                    jumpList.Items.Remove(itemN);
                    jumpList.Items.Insert(0, itemN);
                }
                else
                {
                    JumpListItem itemN = JumpListItem.CreateWithArguments("open:" + helper.Id, helper.Name);
                    itemN.GroupName = "Projekte";
                    jumpList.Items.Insert(0, itemN);
                }

                try
                {
                    await jumpList.SaveAsync();
                }
                catch
                {
                    Log.Warning("Jumpliste konnte nicht gespeichert werden.");
                }
            }

            if(helper.IsReconstruct)
                App.AppFrame.Navigate(typeof(Reconstruct), project);
            else
                App.AppFrame.Navigate(typeof(WorkdeskEasy), project);
        }


        private void DeleteProject(object sender, RoutedEventArgs e)
        {
            ProjectViewHelper proj = (ProjectViewHelper)(sender as Button).DataContext;
            ProjectList.Remove(proj);
            _contextL.Projects.Remove(proj.Local);
            _contextL.SaveChanges();

            SaveHelper.DeleteProject(proj);
            Notify.Show(loader.GetString("MsgProjectDeleted"), 3000);

            Serilog.Log.Information("Projekt wurde gelöscht: " + proj.Id + " - " + proj.Name);
        }

        private async void ClickChangePicFile(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");
            StorageFile file = await picker.PickSingleFileAsync();
            _ = Cropper.LoadImageFromFile(file);
            Cropper.Visibility = Visibility.Visible;
            CropperStandard.Visibility = Visibility.Collapsed;
        }

        private void ClickChangePicStandard(object sender, RoutedEventArgs e)
        {
            string id = ((ComboBoxItem)DiagStandard.SelectedItem).Tag.ToString();

            BitmapImage image = new BitmapImage() { UriSource = new Uri("ms-appx:///Assets/ProjectImgs/" + id + ".png") };
            CropperStandard.Source = image;

            Cropper.Visibility = Visibility.Collapsed;
            CropperStandard.Visibility = Visibility.Visible;
        }

        private void CickDiagCancel(object sender, RoutedEventArgs e)
        {
            DiagNew.Visibility = Visibility.Collapsed;
        }

        private async void CickDiagCreate(object sender, RoutedEventArgs e)
        {
            LoadScreen.IsLoading = true;
            await Task.Delay(1000);
            string tag = (sender as Button).Tag.ToString();

            Project proj = new Project(InName.Text);
            proj.Connection = (LocalConnectionProject)InConn.SelectedItem;

            if(tag == "new")
            {
                Line Backbone = new Line(1, loaderG.GetString("Area"));
                Backbone.Subs.Add(new LineMiddle(1, loaderG.GetString("Line") + " 1", Backbone));
                proj.Lines.Add(Backbone);
            }

            WriteableBitmap image;
            if (Cropper.Visibility == Visibility.Visible)
            {
                StorageFile file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("tempProjImg.png", CreationCollisionOption.ReplaceExisting);
                await Cropper.SaveAsync(await file.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.None), Microsoft.Toolkit.Uwp.UI.Controls.BitmapFileFormat.Png);
                image = new WriteableBitmap((int)Cropper.CroppedRegion.Width, (int)Cropper.CroppedRegion.Height);
                image.SetSource(await file.OpenReadAsync());

                WriteableBitmap newImage;

                using (var fileStream = await file.OpenReadAsync())
                {
                    var bitmap = new BitmapImage();
                    bitmap.SetSource(fileStream);

                    newImage = new WriteableBitmap(512, 512);
                    fileStream.Seek(0);
                    var decoder = await BitmapDecoder.CreateAsync(fileStream);

                    var transform = new BitmapTransform()
                    {
                        ScaledWidth = Convert.ToUInt32(newImage.PixelWidth),
                        ScaledHeight = Convert.ToUInt32(newImage.PixelHeight)
                    };
                    var pixelData = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Straight,
                        transform,
                        ExifOrientationMode.IgnoreExifOrientation,
                        ColorManagementMode.DoNotColorManage
                    );
                    var sourcePixels = pixelData.DetachPixelData();

                    using (var stream = newImage.PixelBuffer.AsStream())
                    {
                        await stream.WriteAsync(sourcePixels, 0, sourcePixels.Length);
                    }
                }

                image = newImage;

                await file.DeleteAsync();
            }
            else
            {
                BitmapImage bmp = (BitmapImage)CropperStandard.Source;
                RandomAccessStreamReference random = RandomAccessStreamReference.CreateFromUri(bmp.UriSour‌​ce);
                using (IRandomAccessStream stream = await random.OpenReadAsync())
                {
                    image = new WriteableBitmap((int)bmp.PixelWidth, (int)bmp.PixelHeight);
                    await image.SetSourceAsync(stream);
                }

            }
            


            byte[] pixels;
            using (Stream stream = image.PixelBuffer.AsStream())
            {
                pixels = new byte[(uint)stream.Length];
                await stream.ReadAsync(pixels, 0, pixels.Length);
            }
            proj.Image = pixels;

            proj.Id = SaveHelper.SaveProject(proj).Id;


            LocalProject lp = new LocalProject();
            lp.ProjectId = proj.Id;
            lp.Name = proj.Name;
            lp.Thumbnail = proj.Image;
            lp.ConnectionId = proj.Connection.Id;
            lp.IsReconstruct = tag == "rec";
            _contextL.Projects.Add(lp);
            _contextL.SaveChanges();

            proj.Local = lp;

            ChangeHandler.Instance = new ChangeHandler(proj.Id);

            Serilog.Log.Information("Projekt wird geöffnet: " + proj.Id + " - " + proj.Name + " / " + tag);

            Analytics.TrackEvent("Projekt erstellt");

            switch (tag)
            {
                case "new":
                    App.AppFrame.Navigate(typeof(WorkdeskEasy), proj);
                    break;
                case "rec":
                    App.AppFrame.Navigate(typeof(Reconstruct), proj);
                    break;
            }
        }

        private void GridTemplate_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ProjectViewHelper helper = (sender as Grid).DataContext as ProjectViewHelper;
            DoOpenProject(helper);
        }

        private void OpenArchive(object sender, RoutedEventArgs e)
        {
            _= Launcher.LaunchFolderPathAsync(ApplicationData.Current.LocalFolder.Path + "\\Logs");
        }

        private void GridTemplate_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            _currentFlyout = FlyoutBase.GetAttachedFlyout((FrameworkElement)sender);
            _currentFlyout.ShowAt((FrameworkElement)sender);
        }

        private void GridItemTapped(object sender, TappedRoutedEventArgs e)
        {

        }
    }
}
