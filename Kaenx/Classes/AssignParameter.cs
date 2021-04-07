using Kaenx.Classes.Dynamic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes
{
    public class AssignParameter { 
        [JsonProperty("t")]
        public string Target { get; set; }
        [JsonProperty("s")]
        public string Source { get; set; }
        [JsonProperty("v")]
        public string Value { get; set; }

        [JsonProperty("w")]
        public bool wasTrue { get; set; }
        [JsonProperty("c")]
        public List<ParamCondition> Conditions { get; set; }
    }
}
