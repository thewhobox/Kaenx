using Kaenx.Classes.Project;
using Serilog;
using System;
using System.Collections.Generic;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class Home : Page
    {
        public Home()
        {
            this.InitializeComponent();
            LazyLoad();
        }

        private async void LazyLoad()
        {
            try
            {
                await Task.Delay(10);

                Project project = (Project)this.DataContext;

                if (project.Image != null)
                {
                    var wb = new WriteableBitmap(512, 512);
                    using (Stream stream = wb.PixelBuffer.AsStream())
                    {
                        await stream.WriteAsync(project.Image, 0, project.Image.Length);
                    }
                    ProjectImage.Source = wb;
                }
            } catch(Exception ex)
            {
                Log.Error(ex, "Failed loading Image");
            }
        }
    }
}
