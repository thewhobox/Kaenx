using Kaenx.Classes;
using Kaenx.Classes.Controls;
using Kaenx.Classes.Helper;
using Kaenx.Classes.Project;
using Kaenx.DataContext.Project;
using Kaenx.View.Controls;
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
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
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
                ProjectViewHelper helper = new ProjectViewHelper();
                helper.Id = model.Id;
                helper.Name = model.Name;

                if(model.Image != null)
                {
                    var wb = new WriteableBitmap(model.ImageW, model.ImageH);
                    using (Stream stream = wb.PixelBuffer.AsStream())
                    {
                        await stream.WriteAsync(model.Image, 0, model.Image.Length);
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
                Log.Error("App ist in letzter Sitzung abgestürzt!", report.StackTrace);
                Notify.Show(loader.GetString("AppCrashed"));
            }
        }

        private void OpenCatalog(object sender, RoutedEventArgs e)
        {
            Serilog.Log.Debug("Katalog öffnen");
            App.Navigate(typeof(Catalog), "main");
        }

        private void OpenNewProjekt(object sender, RoutedEventArgs e)
        {
            DiagNew.Visibility = Visibility;
            InName.Focus(FocusState.Pointer);
            InName.SelectAll();
        }


        private async void OpenProject(object sender, RoutedEventArgs e)
        {
            LoadScreen.IsLoading = true;
            await Task.Delay(200);

            Project project = SaveHelper.LoadProject(((ProjectViewHelper)TestGrid.SelectedItem).Id);
            ChangeHandler.Instance = new ChangeHandler(project.Id);

            Serilog.Log.Debug("Neues Projekt erstellt: " + project.Id + " - " + project.Name);

            App.AppFrame.Navigate(typeof(WorkdeskEasy), project);
        }


        private void DeleteProject(object sender, RoutedEventArgs e)
        {
            ProjectViewHelper proj = (ProjectViewHelper)TestGrid.SelectedItem;
            ProjectList.Remove(proj);

            SaveHelper.DeleteProject(proj.Id);
            Notify.Show(loader.GetString("MsgProjectDeleted"), 3000);

            Serilog.Log.Debug("Projekt wurde gelöscht: " + proj.Id + " - " + proj.Name);
        }

        //private async void ClickOpen(object sender, RoutedEventArgs e)
        //{
        //    if (LBProjects.SelectedIndex < 0)
        //    {
        //        Notify.Show(loader.GetString("MsgSelectProject"), 3000);
        //        return;
        //    }

        //    LoadScreen.IsLoading = true;

        //    await Task.Delay(200);

        //    Project project = SaveHelper.LoadProject(((ProjectModel)LBProjects.SelectedItem).Id);
        //    ChangeHandler.Instance = new ChangeHandler(project.Id);

        //    Serilog.Log.Debug("Neues Projekt erstellt: " + project.Id + " - " + project.Name);

        //    App.AppFrame.Navigate(typeof(WorkdeskEasy), project);
        //}

        private void wizardP_Click(object sender, RoutedEventArgs e)
        {
            Crashes.GenerateTestCrash();
        }

        private void GridItemTapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
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

            Project proj = new Project(InName.Text);
            Line Backbone = new Line(1, loaderG.GetString("Area"));
            Backbone.Subs.Add(new LineMiddle(1, loaderG.GetString("Line") + " 1", Backbone));
            proj.Lines.Add(Backbone);

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

                    // Scale image to appropriate size 
                    var transform = new BitmapTransform()
                    {
                        ScaledWidth = Convert.ToUInt32(newImage.PixelWidth),
                        ScaledHeight = Convert.ToUInt32(newImage.PixelHeight)
                    };
                    var pixelData = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8, // WriteableBitmap uses BGRA format 
                        BitmapAlphaMode.Straight,
                        transform,
                        ExifOrientationMode.IgnoreExifOrientation, // This sample ignores Exif orientation 
                        ColorManagementMode.DoNotColorManage
                    );

                    // An array containing the decoded image data, which could be modified before being displayed 
                    var sourcePixels = pixelData.DetachPixelData();

                    // Open a stream to copy the image contents to the WriteableBitmap's pixel buffer 
                    using (var stream = newImage.PixelBuffer.AsStream())
                    {
                        await stream.WriteAsync(sourcePixels, 0, sourcePixels.Length);
                    }
                }

                image = newImage;


                //StorageFolder pictureFolder = ApplicationData.Current.LocalFolder;
                //var file2 = await pictureFolder.CreateFileAsync("test.jpg", CreationCollisionOption.ReplaceExisting);

                //using (var stream = await file2.OpenStreamForWriteAsync())
                //{
                //    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream.AsRandomAccessStream());
                //    var pixelStream = image.PixelBuffer.AsStream();
                //    byte[] pixels2 = new byte[image.PixelBuffer.Length];

                //    await pixelStream.ReadAsync(pixels2, 0, pixels2.Length);

                //    encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)image.PixelWidth, (uint)image.PixelHeight, 96, 96, pixels2);

                //    await encoder.FlushAsync();
                //}

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
            proj.ImageH = image.PixelHeight;
            proj.ImageW = image.PixelWidth;
            proj.Id = SaveHelper.SaveProject(proj).Id;


            StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Projects", CreationCollisionOption.OpenIfExists);
            await folder.CreateFolderAsync(proj.Id.ToString(), CreationCollisionOption.ReplaceExisting);

            ChangeHandler.Instance = new ChangeHandler(proj.Id);

            Serilog.Log.Debug("Projekt wird geöffnet: " + proj.Id + " - " + proj.Name);

            App.AppFrame.Navigate(typeof(WorkdeskEasy), proj);
        }
    }
}
