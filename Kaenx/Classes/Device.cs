using Kaenx.Classes.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;

namespace Kaenx.Classes
{
    public class Device
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Info { get; set; }
        public Visibility InfoVisible { get { return string.IsNullOrEmpty(Info) ? Visibility.Collapsed : Visibility.Visible; } }
        public string VisibleDescription { get; set; }
        public ICommand LeftCommand { get; set; }
        public bool IsEnabled { get; set; }

        public string ProductRefId { get; set; }
        public string Hardware2ProgramRefId { get; set; }

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
        private SlideListItemBase slideSettings = new SlideListItemBase() { IsSelected = false };
    }
}
