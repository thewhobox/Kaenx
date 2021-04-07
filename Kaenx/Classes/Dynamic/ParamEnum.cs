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
    public class ParamEnum : IDynParameter
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Hash { get; set; }
        public string SuffixText { get; set; }
        public string Default { get; set; }

        private string _value;
        public string Value
        {
            get { return _value; }
            set { if (string.IsNullOrEmpty(value)) return; _value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value")); }
        }

        private Visibility _visible;
        public Visibility Visible
        {
            get { return _visible; }
            set { _visible = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Visible")); }
        }

        public bool HasAccess { get; set; }
        [JsonProperty("o")]
        public List<ParamEnumOption> Options { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<ParamCondition> Conditions { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
