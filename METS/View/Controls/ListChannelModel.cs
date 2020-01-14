using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace METS.View.Controls
{
    public class ListChannelModel
    {
        public string Name { get; set; }
        public ObservableCollection<ListBlockModel> Blocks { get; set; } = new ObservableCollection<ListBlockModel>();
    }
}
