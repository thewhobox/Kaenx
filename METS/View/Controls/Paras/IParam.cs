using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace METS.Classes.Controls.Paras
{
    public interface IParam
    {
        string GetValue();
        void SetVisibility(Visibility visible);
    }
}
