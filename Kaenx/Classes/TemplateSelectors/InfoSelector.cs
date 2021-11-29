using Kaenx.Classes.Bus.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Kaenx.Classes.TemplateSelectors
{
    public class InfoSelector : DataTemplateSelector
    {
        public DataTemplate Error { get; set; }
        public DataTemplate Info { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            switch (item)
            {
                case DeviceInfoData cb:
                    return Info;
            }

            return Error;
        }
    }
}
