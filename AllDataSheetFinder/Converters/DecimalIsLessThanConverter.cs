using System;
using System.Windows.Data;

namespace AllDataSheetFinder.Converters
{
    public class DecimalIsLessThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            decimal x = (decimal)value;
            decimal cutoff = (decimal)parameter;

            return (x < cutoff);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
