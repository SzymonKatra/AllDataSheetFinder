using System;
using System.Windows;
using System.Windows.Data;

namespace AllDataSheetFinder.Converters
{
    public class PartMoreInfoStateToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            PartMoreInfoState current = (PartMoreInfoState)value;
            PartMoreInfoState target = (PartMoreInfoState)parameter;

            return (current == target ? Visibility.Visible : Visibility.Collapsed);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
