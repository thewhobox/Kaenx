using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Kaenx.View.Controls
{
    public class ListBlockModel : INotifyPropertyChanged
    {
        private string _name;
        public string Id { get; set; }
        public string Name
        {
            get { return _name; }
            set { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); }
        }
        public StackPanel Panel { get; set; }
        public Visibility Visible { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
