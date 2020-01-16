using METS.Classes.Controls.Paras;
using METS.Context.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.Classes.Helper
{
    public class ParamVisHelper
    {
        public AppParameter Parameter { get; set; }
        public string Hash { get; set; }
        public List<ParamCondition> Conditions { get; set; }


        public ParamVisHelper(AppParameter para)
        {
            Parameter = para;
        }
    }
}
