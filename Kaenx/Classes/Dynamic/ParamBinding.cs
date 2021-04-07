using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Dynamic
{
    public class ParamBinding
    {
        [JsonProperty("s")]
        public string SourceId { get; set; }
        [JsonProperty("t")]
        public string DefaultText { get; set; }
        [JsonProperty("h")]
        public string Hash { get; set; }
    }
}