using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Dynamic
{
    public class ParamColor : IDynParameter
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string Hash { get; set; }
        public string SuffixText { get; set; }
        public string Default { get; set; }

        public string Value
        {
            get { return ColorHelper.ToHex(Color).Substring(3); }
            set { 
                if (string.IsNullOrEmpty(value)) return;
                try
                {
                    Color = ColorHelper.ToColor("#" + value);
                } catch
                {
                    Kaenx.Classes.Helper.ViewHelper.Instance.ShowNotification("main", "Eingegebene Farbe ist inkorrekt!", 3000, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                    return;
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color"));
            }
        }

        private Visibility _visible;
        public Visibility Visible
        {
            get { return _visible; }
            set { _visible = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Visible")); }
        }

        private Windows.UI.Color _color;
        [JsonProperty("co")]
        public Windows.UI.Color Color
        {
            get { return _color; }
            set { _color = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color")); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value")); }
        }

        public Visibility SuffixIsVisible { get { return string.IsNullOrEmpty(SuffixText) ? Visibility.Collapsed : Visibility.Visible; } }

        public bool HasAccess { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<ParamCondition> Conditions { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
