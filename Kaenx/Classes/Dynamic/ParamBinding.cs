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
        public int SourceId { get; set; } = -2;
        [JsonProperty("t")]
        public string DefaultText { get; set; }
        [JsonProperty("h")]
        public string Hash { get; set; }
    }
}