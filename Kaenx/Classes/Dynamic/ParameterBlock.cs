using Kaenx.Classes.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Dynamic
{
    public class ParameterBlock: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string Id { get; set; }
        public string Text { get; set; }
        public bool HasAccess { get; set; } = true;

        private string _dtext;
        public string DisplayText
        {
            get { return _dtext; }
            set
            {
                _dtext = value;
                Changed("DisplayText");
            }
        }

        private Visibility _visible;
        public Visibility Visible
        {
            get { return _visible; }
            set { 
                _visible = value; 
                Changed("Visible"); 
            }
        }

        public List<ParamCondition> Conditions { get; set; } = new List<ParamCondition>();
        public List<IDynParameter> Parameters { get; set; } = new List<IDynParameter>();
    }
}
