﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Kaenx.Classes.Dynamic
{
    public class ParamSeperatorBox : IDynParameter
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Hash { get; set; }
        public string SuffixText { get; set; }
        public string Default { get; set; }
        public string Hint { get; set; }

        public string Value { get; set; }

        private Visibility _visible;
        public Visibility Visible
        {
            get { return _visible; }
            set { _visible = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Visible")); }
        }

        //public SolidColorBrush BBorder { get { return new SolidColorBrush(Hint == "Error" ? Colors.Red : Colors.Blue); } }
        //public SolidColorBrush BBackground { get { return new SolidColorBrush(Hint == "Error" ? Colors.Red : Colors.Blue) { Opacity = 0.4 }; } }

        public Visibility SuffixIsVisible { get { return Visibility.Collapsed; } }

        public List<ParamCondition> Conditions { get; set; }
        public bool IsEnabled { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
