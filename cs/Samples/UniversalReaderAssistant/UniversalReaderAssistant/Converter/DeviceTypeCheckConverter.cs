using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ThingMagic.Converters
{
    public class DeviceTypeCheckConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            else if (value.ToString().ToLower().Contains("add device manually"))
            {
                return "Manual";
            }

            else if (value.ToString().ToLower().Contains("com"))
            {
                return "Serial";
            }
            else if (value.ToString().ToLower().Contains("network"))
            {
                return "Network";
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
