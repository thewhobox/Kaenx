using METS.Context.Catalog;
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

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace METS.Classes.Controls.Paras
{
    public sealed partial class ParamNone : UserControl, IParam
    {
        public string hash { get; set; }

        public ParamNone(AppParameter param, AppParameterTypeViewModel type)
        {
            this.InitializeComponent();
            ParaName.Text = param.Text;
            if (string.IsNullOrWhiteSpace(ParaName.Text))
                ParaBorder.Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
        }

        public ParamNone(string text)
        {
            this.InitializeComponent();
            ParaName.Text = text;
            if (string.IsNullOrWhiteSpace(ParaName.Text))
                ParaBorder.Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
        }

        public string GetValue()
        {
            throw new NotImplementedException();
        }

        public void SetVisibility(Visibility visible)
        {
            this.Visibility = visible;
        }
    }
}
