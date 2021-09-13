using Kaenx.Classes.Helper;
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
    public class ParameterBlock: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        [JsonProperty("i")]
        public int Id { get; set; }
        [JsonProperty("t")]
        public string Text { get; set; }
        [JsonProperty("d")]
        public string DefaultText { get; set; }
        [JsonProperty("a")]
        public bool HasAccess { get; set; } = true;

        private string _dtext;
        [JsonProperty("di")]
        public string DisplayText
        {
            get { return _dtext; }
            set
            {
                _dtext = value;
                Changed("DisplayText");
            }
        }


        private Visibility _visible;
        [JsonProperty("v")]
        public Visibility Visible
        {
            get { return _visible; }
            set { 
                _visible = value; 
                Changed("Visible"); 
            }
        }

        [JsonProperty("c")]
        public List<ParamCondition> Conditions { get; set; } = new List<ParamCondition>();
        [JsonProperty("p")]
        public List<IDynParameter> Parameters { get; set; } = new List<IDynParameter>();
    }
}
