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
    public class ParamSeperator : IDynParameter
    {
        public int Id { get; set; }
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

        [JsonIgnore]
        [JsonProperty("l")]
        public Visibility IsLineVisible { get { return Hint == "HorizontalRuler" ? Visibility.Visible : Visibility.Collapsed; } }

        public bool HasAccess { get; set; } = true;
        public List<ParamCondition> Conditions { get; set; }
        public bool IsEnabled { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
