using Kaenx.Classes.Controls.Paras;
using Kaenx.DataContext.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Helper
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
