using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Dynamic
{
    public class ParamText : IDynParameter
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string SuffixText { get; set; }
        public string Value { get; set; }
        public string Default { get; set; }

        public int MaxLength { get; set; }

        public Visibility SuffixIsVisible { get { return string.IsNullOrEmpty(SuffixText) ? Visibility.Collapsed : Visibility.Visible; } }

        public List<ParamCondition> Conditions { get; set; }
    }
}
