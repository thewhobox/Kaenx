using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Kaenx.Classes.Converter
{
    public class InterfaceTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var x = value as Type;

            if(x == typeof(Konnect.Interfaces.KnxInterfaceIp)) //IP Interface
            {
                return "Ip Interface"; //EC27
            } else if(x == typeof(Konnect.Interfaces.KnxInterfaceRemote))
            {
                return "Remote Verbindung";
            } else if(x == typeof(Konnect.Interfaces.KnxInterfaceUsb))
            {
                return "USB";
            }/* else if(x == typeof(Konnect.Interfaces.KnxInterfaceIpRouter))
            {
                return "IP Router";
            }*/
            //Todo add IpRouter

            return "Unbekannt";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
