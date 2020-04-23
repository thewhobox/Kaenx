using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Dynamic
{
    public interface IDynChannel
    {
        public string Id { get; set; }
        public List<ParameterBlock> Blocks { get; set; }
        public Visibility Visible { get; set; }

        public List<ParamCondition> Conditions { get; set; }
    }
}
