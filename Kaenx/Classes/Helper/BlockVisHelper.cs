using Kaenx.Classes.Controls.Paras;
using Kaenx.View.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes.Helper
{
    public class BlockVisHelper
    {
        public ListBlockModel Block { get; set; }
        public string Hash { get; set; }
        public List<Dynamic.ParamCondition> Conditions { get; set; }


        public BlockVisHelper(ListBlockModel model)
        {
            Block = model;
        }
    }
}
