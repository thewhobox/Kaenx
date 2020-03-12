using Kaenx.DataContext;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    public sealed partial class ParamEnum : UserControl, IParam
    {
        public string Hash { get; set; }
        public string ParamId { get; }
        public delegate void ParamChangedHandler(IParam param);
        public event ParamChangedHandler ParamChanged;

        private ObservableCollection<AppParameterTypeEnumViewModel> EnumList { get; set; } = new ObservableCollection<AppParameterTypeEnumViewModel>();


        public ParamEnum(AppParameter param, AppParameterTypeViewModel type, IEnumerable<AppParameterTypeEnumViewModel> enums)
        {
            this.InitializeComponent();
            this.DataContext = EnumList;

            ParamId = param.Id;
            ParaName.Text = param.Text;

            foreach (AppParameterTypeEnumViewModel model in enums)
            {
                EnumList.Add(model);
            }

            ToolTip toolTip = new ToolTip();
            ParaValue.SelectedValue = param.Value;

            foreach(AppParameterTypeEnumViewModel e in enums)
            {
                if (e.Value == param.Value) toolTip.Content = "Default: " + e.Text;
            }

            ToolTipService.SetToolTip(ParaValue, toolTip);
            ParaValue.SelectionChanged += ParaValue_SelectionChanged;
        }

        private void ParaValue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ParamChanged?.Invoke(this);
        }

        public string GetValue()
        {
            return ParaValue.SelectedValue.ToString();
        }

        public void SetVisibility(Visibility visible)
        {
            this.Visibility = visible;
        }

        public void SetValue(string val)
        {
            ParaValue.SelectedValue = val;
        }
    }
}
