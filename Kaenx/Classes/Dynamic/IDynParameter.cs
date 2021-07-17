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
    public interface IDynParameter: INotifyPropertyChanged
    {
        [JsonProperty("i")]
        public int Id { get; set; }
        [JsonProperty("t")]
        public string Text { get; set; }
        [JsonProperty("h")]
        public string Hash { get; set; }
        [JsonProperty("s")]
        public string SuffixText { get; set; }
        [JsonProperty("v")]
        public string Value { get; set; }

        [JsonProperty("d")]
        public string Default { get; set; }

        [JsonProperty("a")]
        public bool HasAccess { get; set; }
        [JsonProperty("e")]
        public bool IsEnabled { get; set; }
        [JsonProperty("vi")]
        public Visibility Visible { get; set; }
        [JsonProperty("c")]
        public List<ParamCondition> Conditions { get; set; }
    }
}
