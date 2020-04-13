using Kaenx.Classes.Bus.Data;
using Kaenx.Views.Easy.Controls;
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
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Kaenx.View.Controls
{
    public sealed partial class DiagDeviceConfig : ContentDialog
    {
        public DiagDeviceConfig(DeviceConfigData data)
        {
            this.InitializeComponent();

            EControlParas2 ctrl = new EControlParas2(data);
            this.Content = ctrl;
            DelayStart(ctrl, data);
        }

        public async void DelayStart(EControlParas2 ctrl, DeviceConfigData data)
        {
            await Task.Delay(1000);
            ctrl.StartRead();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
