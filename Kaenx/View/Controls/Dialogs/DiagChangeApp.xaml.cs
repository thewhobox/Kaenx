using Kaenx.Classes.Project;
using Kaenx.DataContext.Catalog;
using Kaenx.MVVM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class DiagChangeApp : ContentDialog
    {
        public ApplicationViewModel SelectedApp { get; set; }

        private LineDevice _device;

        public DiagChangeApp(LineDevice device)
        {
            this.InitializeComponent();

            _device = device;

            using(CatalogContext context = new CatalogContext())
            {
                List<ChangeAppModel> models = new List<ChangeAppModel>();

                foreach(ApplicationViewModel app in context.Applications.Where(a => a.HardwareId == device.HardwareId))
                {
                    models.Add(new ChangeAppModel(app));
                }
                AppList.ItemsSource = models;
            }

        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SelectedApp = AppList.SelectedItem as ApplicationViewModel;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
