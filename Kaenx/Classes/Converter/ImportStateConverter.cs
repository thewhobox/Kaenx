using Kaenx.DataContext.Import;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Kaenx.Classes.Converter
{
    public class ImportStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ImportState state = (ImportState)value;
            switch(state)
            {
                case ImportState.Waiting:
                    return Colors.Transparent;

                case ImportState.Importing:
                    return Colors.Orange;

                case ImportState.Finished:
                    return Colors.Green;

                case ImportState.Error:
                    return Colors.Red;
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((bool)value) ? "1" : "0";
        }
    }
}
