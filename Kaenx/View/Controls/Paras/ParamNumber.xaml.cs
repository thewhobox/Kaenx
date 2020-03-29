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
    public sealed partial class ParamNumber : UserControl, IParam
    {
        public string Hash { get; set; }
        public string ParamId { get; }
        public delegate void ParamChangedHandler(IParam param);
        public event ParamChangedHandler ParamChanged;


        public ParamNumber(AppParameter param, AppParameterTypeViewModel type)
        {
            this.InitializeComponent();

            ParamId = param.Id;
            ParaName.Text = param.Text;

            ParaValue.Value = int.Parse(param.Value);

            //ParaValue.ValueChanged += ParaValue_ValueChanged;
            ParaValue.LostFocus += ParaValue_LostFocus;

            ParaValue.Minimum = int.Parse(type.Tag1);
            ParaValue.Maximum = int.Parse(type.Tag2);



            ToolTipService.SetToolTip(ParaValue, type.Tag1 + " - " + type.Tag2);
        }

        private void ParaValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ParamChanged?.Invoke(this);
        }

        private void ParaValue_ValueChanged()
        {
            ParamChanged?.Invoke(this);
        }

        public string GetValue()
        {
            return ParaValue.Value.ToString();
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
            ParaValue.Value = int.Parse(val);
        }
    }
}
