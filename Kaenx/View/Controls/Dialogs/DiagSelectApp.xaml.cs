using Kaenx.DataContext.Catalog;
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

namespace Kaenx.Classes.Controls
{
    public sealed partial class DiagSelectApp : ContentDialog
    {
        public int ApplicationId { get; set; }

        public DiagSelectApp(int hardwareId)
        {
            this.InitializeComponent();

            CatalogContext context = new CatalogContext();

            IEnumerable<ApplicationViewModel> apps = context.Applications.Where(h => h.HardwareId == hardwareId).OrderByDescending(h => h.Version);
            Apps.ItemsSource = apps;
            Apps.SelectedIndex = 0;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ApplicationId = ((Hardware2AppModel)Apps.SelectedItem).ApplicationId;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ApplicationId = -1;
        }
    }
}
