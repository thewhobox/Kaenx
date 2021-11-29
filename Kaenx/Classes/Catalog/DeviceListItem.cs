using Kaenx.Classes.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Kaenx.Classes.Catalog
{
    public class DeviceListItem
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public string Info { get; set; }

        public Visibility InfoVisible
        {
            get { return string.IsNullOrEmpty(Info) ? Visibility.Collapsed : Visibility.Visible; }
        }

        public SlideListItemBase SlideSettings
        {
            get
            {
                return slideSettings;
            }
            set
            {
                slideSettings = value;
            }
        }
        private SlideListItemBase slideSettings = new SlideListItemBase();
    }
}
