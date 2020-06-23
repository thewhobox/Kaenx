using Kaenx.Classes.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes
{
    public class AssignParameter
    {
        public string Target { get; set; }
        public string Source { get; set; }
        public string Value { get; set; }

        public bool wasTrue { get; set; }
        public List<ParamCondition> Conditions { get; set; }
    }
}
