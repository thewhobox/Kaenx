using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Dynamic
{
    public class ViewParamModel
    {
        public string Value { get; set; }
        public List<IDynParameter> Parameters { get; set; } = new List<IDynParameter>();
        public AssignParameter Assign { get; set; }

        public ViewParamModel(string value)
        {
            Value = value;
        }
    }
}
