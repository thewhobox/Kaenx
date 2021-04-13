using Kaenx.Classes.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Kaenx.Classes.Converter
{
    public class StateBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            LineState state = (LineState)value;

            switch (state)
            {
                case LineState.Normal:
                    return new SolidColorBrush(Windows.UI.Colors.Transparent);

                case LineState.Warning:
                    return new SolidColorBrush(Windows.UI.Colors.Orange);

                case LineState.Overloaded:
                    return new SolidColorBrush(Windows.UI.Colors.Red);
            }


            return new SolidColorBrush(Windows.UI.Colors.Blue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
