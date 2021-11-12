using Kaenx.DataContext.Import.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Kaenx.Classes.TemplateSelectors
{
    public class BlockSelector : DataTemplateSelector
    {
        public DataTemplate Channel { get; set; }
        public DataTemplate Independent { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            switch (item)
            {
                case ChannelBlock cb:
                    return Channel;

                case ChannelIndependentBlock cib:
                    return Independent;
            }

            return Independent;
        }
    }
}
