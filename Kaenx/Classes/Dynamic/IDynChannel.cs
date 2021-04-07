using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Dynamic
{
    public interface IDynChannel
    {
        public string Id { get; set; }
        [JsonProperty("ha")]
        public bool HasAccess { get; set; }
        [JsonProperty("bl")]
        public List<ParameterBlock> Blocks { get; set; }
        [JsonProperty("vi")]
        public Visibility Visible { get; set; }

        [JsonProperty("co")]
        public List<ParamCondition> Conditions { get; set; }
    }
}
