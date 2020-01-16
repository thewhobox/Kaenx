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
    public sealed partial class ParamNumber : UserControl, IParam
    {
        public string hash { get; set; }
        public delegate void ParamChangedHandler(string source, string value, string hash);
        public event ParamChangedHandler ParamChanged;

        private string paraId;

        public ParamNumber(AppParameter param, AppParameterTypeViewModel type)
        {
            this.InitializeComponent();

            ParaValue.Text = param.Value;
            paraId = param.Id;
            ParaName.Text = param.Text;

            ParaValue.ValueChanged += ParaValue_ValueChanged;
            ParaValue.LostFocus += ParaValue_LostFocus;

            ParaValue.Minimum = int.Parse(type.Tag1);
            ParaValue.Maximum = int.Parse(type.Tag2);

            ToolTipService.SetToolTip(ParaValue, type.Tag1 + " - " + type.Tag2);
        }

        private void ParaValue_LostFocus(object sender, RoutedEventArgs e)
        {
            ParamChanged?.Invoke(paraId, ParaValue.Value.ToString(), hash);
        }

        private void ParaValue_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            ParamChanged?.Invoke(paraId, ParaValue.Value.ToString(), hash);
        }

        public string GetValue()
        {
            return ParaValue.Value.ToString();
        }

        public void SetVisibility(Visibility visible)
        {
            this.Visibility = visible;
        }
    }
}
