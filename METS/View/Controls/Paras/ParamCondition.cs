using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.Classes.Controls.Paras
{
    public class ParamCondition
    {
        public string SourceId { get; set; }
        public string DestinationId { get; set; }
        public List<string> Values { get; set; } = new List<string>();

        public ParamCondition() { }
    }

}
