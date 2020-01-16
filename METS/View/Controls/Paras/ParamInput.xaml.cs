using METS.Context;
using METS.Context.Catalog;
using METS.Context.Project;
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
    public sealed partial class ParamInput : UserControl, IParam
    {
        public string hash { get; set; }
        public delegate void ParamChangedHandler(string source, string value, string hash);
        public event ParamChangedHandler ParamChanged;

        private string paraId;

        public string Name2
        {
            get { return ParaName.Text; }
            set { ParaName.Text = value; }
        }

        public ParamInput(AppParameter param, AppParameterTypeViewModel type)
        {
            this.InitializeComponent();
            ParaName.Text = param.Text;
            ParaValue.Text = param.Value;
            paraId = param.Id;

            ParaValue.KeyUp += ParaValue_KeyUp;
            ParaValue.LostFocus += ParaValue_LostFocus;
        }

        private void ParaValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ParamChanged?.Invoke(paraId, ParaValue.Text, hash);
        }

        private void ParaValue_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                ParamChanged?.Invoke(paraId, ParaValue.Text, hash);
        }

        public string GetValue()
        {
            return ParaValue.Text;
        }

        public void SetVisibility(Visibility visible)
        {
            this.Visibility = visible;
        }
    }
}
