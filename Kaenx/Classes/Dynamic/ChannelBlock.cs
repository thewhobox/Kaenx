using Kaenx.Classes.Controls.Paras;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Dynamic
{
    public class ChannelBlock : IDynChannel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public int Number { get; set; }

        public List<ParameterBlock> Blocks { get; set; } = new List<ParameterBlock>();
        public List<ParamCondition> Conditions { get; set; } = new List<ParamCondition>();

        private Visibility _visible;
        public Visibility Visible
        {
            get { return _visible; }
            set { _visible = value; Changed("Visible"); }
        }

    }
}
