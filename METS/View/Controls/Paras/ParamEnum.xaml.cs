using METS.Context;
using METS.Context.Catalog;
using METS.Context.Project;
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

namespace METS.Classes.Controls.Paras
{
    public sealed partial class ParamEnum : UserControl, IParam
    {
        public delegate void ParamChangedHandler(string source, string value);
        public event ParamChangedHandler ParamChanged;

        private ObservableCollection<AppParameterTypeEnumViewModel> EnumList { get; set; } = new ObservableCollection<AppParameterTypeEnumViewModel>();

        private string paramId;

        public ParamEnum(AppParameter param, AppParameterTypeViewModel type, IEnumerable<AppParameterTypeEnumViewModel> enums)
        {
            this.InitializeComponent();
            this.DataContext = EnumList;

            paramId = param.Id;
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
            ParamChanged?.Invoke(paramId, ParaValue.SelectedValue.ToString());
        }

        public string GetValue()
        {
            return ParaValue.SelectedValue.ToString();
        }

        public void SetVisibility(Visibility visible)
        {
            this.Visibility = visible;
        }
    }
}
