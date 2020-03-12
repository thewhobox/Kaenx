﻿using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public sealed partial class ParamEnum2 : UserControl, IParam
    {
        public string Hash { get; set; }
        public string ParamId { get; }
        public delegate void ParamChangedHandler(IParam param);
        public event ParamChangedHandler ParamChanged;
        
        private string value;

        public ParamEnum2(AppParameter param, AppParameterTypeViewModel type, IEnumerable<AppParameterTypeEnumViewModel> enums)
        {
            this.InitializeComponent();
            ToolTip toolTip = new ToolTip();

            ParamId = param.Id;
            ParaName.Text = param.Text;
            value = param.Value;

            ParaValue1.Content = enums.ElementAt(0).Text;
            ParaValue1.GroupName = ParamId;
            ParaValue1.Tag = enums.ElementAt(0).Value;
            ParaValue1.IsChecked = value == enums.ElementAt(0).Value;
            ParaValue1.Checked += ParaValue_Checked;
            if (ParaValue1.IsChecked == true)
                toolTip.Content = "Default: " + enums.ElementAt(0).Text;

            ParaValue2.Content = enums.ElementAt(1).Text;
            ParaValue2.GroupName = ParamId;
            ParaValue2.Tag = enums.ElementAt(1).Value;
            ParaValue2.IsChecked = value == enums.ElementAt(1).Value;
            ParaValue2.Checked += ParaValue_Checked;
            if(ParaValue2.IsChecked == true)
                toolTip.Content = "Default: " + enums.ElementAt(1).Text;

            if ((enums.ElementAt(0).Text.Length + enums.ElementAt(1).Text.Length) < 30)
                ValuePanel.Orientation = Orientation.Horizontal;

            ToolTipService.SetToolTip(ValuePanel, toolTip);
        }

        public string GetValue()
        {
            return value;
        }

        private void ParaValue_Checked(object sender, RoutedEventArgs e)
        {
            value = ((RadioButton)sender).Tag.ToString();
            ParamChanged?.Invoke(this);
        }

        public void SetVisibility(Visibility visible)
        {
            this.Visibility = visible;
        }

        public void SetValue(string val)
        {
            if (ParaValue1.Tag.ToString() == val)
                ParaValue1.IsChecked = true;
            if (ParaValue2.Tag.ToString() == val)
                ParaValue2.IsChecked = true;
        }
    }
}
