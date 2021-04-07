using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Dynamic
{
    public class ParamEnumOption
    {
        [JsonProperty("t")]
        public string Text { get; set; }
        [JsonProperty("v")]
        public string Value { get; set; }
    }
}
