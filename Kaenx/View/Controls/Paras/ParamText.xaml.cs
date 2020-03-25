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

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace Kaenx.Classes.Controls.Paras
{
    public sealed partial class ParamText : UserControl, IParam
    {
        public string Hash { get; set; }

        public string Value { get { return ParaValue.Text; } }

        public string ParamId { get; }

        public ParamText(AppParameter param, AppParameterTypeViewModel type)
        {
            this.InitializeComponent();
            ParaName.Text = param.Text;
            ParaValue.Text = param.Value;
            ParamId = param.Id;
        }

        public string GetValue()
        {
            throw new NotImplementedException();
        }

        public void SetVisibility(Visibility visible)
        {
            this.Visibility = visible;
        }

        public Visibility GetVisibility()
        {
            return this.Visibility;
        }

        public void Undo() { }

        public void SetValue(string val)
        {
            ParaValue.Text = val;
        }
    }
}
