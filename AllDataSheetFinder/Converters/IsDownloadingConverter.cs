﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AllDataSheetFinder.Converters
{
    public class IsDownloadingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            PartDatasheetState state = (PartDatasheetState)value;
            return state == PartDatasheetState.Downloading || state == PartDatasheetState.DownloadingAndOpening;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
