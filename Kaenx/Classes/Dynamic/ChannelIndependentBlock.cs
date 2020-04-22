using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Dynamic
{
    public class ChannelIndependentBlock : IDynChannel
    {
        public List<ParameterBlock> Blocks { get; set; } = new List<ParameterBlock>();
        public Visibility Visible { get; set; }

        public List<ParamCondition> Conditions { get; set; } = new List<ParamCondition>();
    }
}
