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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace Kaenx.Classes.Controls.Paras
{
    public sealed partial class ParamPicture : UserControl, IParam
    {
        public string Hash { get; set; }

        public string ParamId => throw new NotImplementedException();

        public ParamPicture(AppParameter param, AppParameterTypeViewModel type)
        {
            this.InitializeComponent();
            ParaName.Text = param.Text;

            BitmapImage img = new BitmapImage(new Uri("ms-appdata:///local/Baggages/" + type.Tag1));
            ParaValue.Source = img;
        }

        public string GetValue()
        {
            throw new NotImplementedException();
        }

        public void SetVisibility(Visibility visible)
        {
            this.Visibility = visible;
        }

        public void SetValue(string val)
        {
            throw new NotImplementedException();
        }
    }
}