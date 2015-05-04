using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace AllDataSheetFinder.Converters
{
    public class PartDatasheetStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            PartDatasheetState state = (PartDatasheetState)value;

            switch(state)
            {
                case PartDatasheetState.Downloading: return Colors.LightBlue;
                case PartDatasheetState.Saved: return Colors.Orange;
                case PartDatasheetState.Cached: return Colors.LightGray;
                default: return Colors.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
