using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.UI.Xaml.Controls;

namespace Kaenx.Classes
{
    public class ParamAfterload
    {
        public XElement element;
        public StackPanel parent;
        public bool loaded = false;

        public ParamAfterload(XElement _ele, StackPanel _parent)
        {
            element = _ele;
            parent = _parent;
        }
    }
}
