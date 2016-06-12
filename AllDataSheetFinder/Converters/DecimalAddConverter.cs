using System;
using System.Globalization;
using System.Windows.Data;

namespace AllDataSheetFinder.Converters
{
    public class DecimalAddConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            decimal x = (decimal)value;
            string p = (string)parameter;
            string[] tokens = p.Split(';');

            if (tokens.Length >= 1)
            {
                decimal delta = decimal.Parse(tokens[0], CultureInfo.InvariantCulture);
                x += delta;

                if(tokens.Length >= 2)
                {
                    decimal cutoff = decimal.Parse(tokens[1], CultureInfo.InvariantCulture);
                    if (delta > 0)
                    {
                        if (x >= cutoff) x = cutoff; 
                    }
                    else // delta < 0
                    {
                        if (x <= cutoff) x = cutoff;
                    }
                }
            }

            return x;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
