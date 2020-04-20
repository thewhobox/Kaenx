using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Dynamic
{
    public interface IDynParameter: INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Hash { get; set; }
        public string SuffixText { get; set; }
        public string Value { get; set; }

        public string Default { get; set; }
         
        public Visibility Visible { get; set; }
        public List<ParamCondition> Conditions { get; set; }
    }
}
