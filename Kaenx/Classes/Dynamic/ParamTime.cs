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
    public class ParamTime : IDynParameter
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string Hash { get; set; }
        public string SuffixText { get; set; }
        public string Default { get; set; }
        public int Divider { get; set; }

        public string TempValue
        {
            get {
                return (int.Parse(Value) * Divider).ToString();
            }
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TempValue"));
                _value = (Math.Floor((double)int.Parse(value) / Divider)).ToString();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
            }
        }

        private string _value;
        public string Value
        {
            get { return _value; }
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TempValue"));
            }
        }


        //private string _tempValue;
        //public string TempValue
        //{
        //    get { return _tempValue; }
        //    set {
        //        if (string.IsNullOrEmpty(value)) return;
        //        _tempValue = value; 
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TempValue"));
        //        _value = (Math.Floor((double)int.Parse(value) / Divider)).ToString();
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));
        //    }
        //}

        //private string _value;
        //public string Value
        //{
        //    get { return _value; }
        //    set { 
        //        if (string.IsNullOrEmpty(value)) return; 
        //        _value = value; 
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Value"));

        //        _tempValue = (int.Parse(value) * Divider).ToString();
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TempValue"));
        //    }
        //}

        private Visibility _visible;
        public Visibility Visible
        {
            get { return _visible; }
            set { _visible = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Visible")); }
        }


        [JsonProperty("mi")]
        public int Minimum { get; set; }
        [JsonProperty("ma")]
        public int Maximum { get; set; }

        public bool HasAccess { get; set; }
        public bool IsEnabled { get; set; } = true;
        public List<ParamCondition> Conditions { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
