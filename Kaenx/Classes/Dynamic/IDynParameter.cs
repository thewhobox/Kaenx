using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Dynamic
{
    public interface IDynParameter
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string SuffixText { get; set; }
        public string Value { get; set; }

        public string Default { get; set; }

        public List<ParamCondition> Conditions { get; set; }
    }
}
