using Kaenx.DataContext.Import;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Kaenx.Classes
{
    public class TVNode : TreeViewNode
    {
        public int SectionId { get; set; }
        public ImportTypes ImportType { get; set; }
    }
}
