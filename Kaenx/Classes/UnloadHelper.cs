using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Kaenx.Classes
{
    public class UnloadHelper
    {
        public bool UnloadApplication { get; set; } = false;
        public bool UnloadAddress { get; set; } = false;
        public bool UnloadBoth { get; set; } = true;
        public Visibility SerialVisible { get; set; } = Visibility.Visible;
        public UnloadOverTypes UnloadOver { get; set; } =  UnloadOverTypes.Address;


        public enum UnloadOverTypes
        {
            Address,
            Serial
        }
    }
}
