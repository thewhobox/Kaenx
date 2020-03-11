using Kaenx.DataContext.Local;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Kaenx.Classes.Helper
{
    public class ProjectViewHelper
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ImageSource Image { get; set; }
        public LocalProject Local { get; set; }
    }
}
