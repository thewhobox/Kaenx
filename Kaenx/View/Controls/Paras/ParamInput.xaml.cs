using Kaenx.DataContext;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
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

namespace Kaenx.Classes.Controls.Paras
{
    public sealed partial class ParamInput : UserControl, IParam
    {
        public string Hash { get; set; }
        public string ParamId { get; }
        public delegate void ParamChangedHandler(IParam param);
        public event ParamChangedHandler ParamChanged;

        public string Name2
        {
            get { return ParaName.Text; }
            set { ParaName.Text = value; }
        }


        public string Value { get { return ParaValue.Text; } }

        public ParamInput(AppParameter param, AppParameterTypeViewModel type)
        {
            this.InitializeComponent();
            ParaName.Text = param.Text;
            ParaValue.Text = param.Value;
            ParamId = param.Id;

            ParaValue.KeyUp += ParaValue_KeyUp;
            ParaValue.LostFocus += ParaValue_LostFocus;


            if (param.Access == AccessType.Read)
                ParaValue.IsEnabled = false;
        }

        private void ParaValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ParamChanged?.Invoke(this);
        }

        private void ParaValue_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                ParamChanged?.Invoke(this);
        }

        public string GetValue()
        {
            return ParaValue.Text;
        }

        public void SetVisibility(Visibility visible)
        {
            this.Visibility = visible;
        }

        public Visibility GetVisibility()
        {
            return this.Visibility;
        }

        public void SetValue(string val)
        {
            ParaValue.Text = val;
        }
    }
}
