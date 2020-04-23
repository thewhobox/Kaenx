using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Dynamic
{
    public class ParamCondition
    {
        public string SourceId { get; set; }
        public string Values { get; set; }
        public ConditionOperation Operation { get; set; }

        public ParamCondition() { }
    }

    public enum ConditionOperation
    {
        IsInValue,
        Default,
        GreatherThan,
        GreatherEqualThan,
        LowerThan,
        LowerEqualThan,
        NotEqual,
        Equal
    }

}
