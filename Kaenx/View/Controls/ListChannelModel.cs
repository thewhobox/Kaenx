using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.View.Controls
{
    public class ListChannelModel : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); }
        }
        public ObservableCollection<ListBlockModel> Blocks { get; set; } = new ObservableCollection<ListBlockModel>();

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
