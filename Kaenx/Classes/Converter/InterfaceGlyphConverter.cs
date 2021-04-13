using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Kaenx.Classes.Converter
{
    public class InterfaceGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var x = value as Type;

            if(x == typeof(Konnect.Interfaces.KnxInterfaceIp)) //IP Interface
            {
                return "\xE968"; //EC27
            } else if(x == typeof(Konnect.Interfaces.KnxInterfaceRemote))
            {
                return "\xE774";
            } else if(x == typeof(Konnect.Interfaces.KnxInterfaceUsb))
            {
                return "\xECF0";
            }/* else if(x == typeof(Konnect.Interfaces.KnxInterfaceIpRouter))
            {
                return "\xE704";
            }*/
            //Todo add IpRouter

            return "\xF142";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
