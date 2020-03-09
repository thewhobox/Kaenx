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
        public string hash { get; set; }
        void SetVisibility(Visibility visible);
    }
}
