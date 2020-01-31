using System;
using System.Globalization;
using System.Windows.Data;

namespace UIMFViewer.FrameControl
{
    public class IndexBase1Converter : IValueConverter
    { public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int base0)
            {
                return base0 + 1;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int base1)
            {
                return base1 - 1;
            }

            return value;
        }
    }
}
