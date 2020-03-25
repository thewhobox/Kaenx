using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Controls.Paras
{
    public interface IParam
    {
        string GetValue();
        void SetValue(string val);
        public string Hash { get; set; }
        public string ParamId { get; }
        void SetVisibility(Visibility visible);
        Visibility GetVisibility();
    }
}
