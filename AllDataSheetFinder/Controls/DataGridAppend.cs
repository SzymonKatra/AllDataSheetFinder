using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AllDataSheetFinder.Controls
{
    public class DataGridAppend : DataGrid
    {
        public DataGridAppend()
        {
            this.Template = (ControlTemplate)Application.Current.TryFindResource("TemplateDataGridAppend");
        }

        public static readonly DependencyProperty AppendControlProperty = DependencyProperty.Register("AppendControl", typeof(object), typeof(DataGridAppend), new PropertyMetadata(null));
        public object AppendControl
        {
            get { return GetValue(AppendControlProperty); }
            set { SetValue(AppendControlProperty, value); }
        }

        public static readonly DependencyProperty EnableSmoothScrollingProperty = DependencyProperty.Register("EnableSmoothScrolling", typeof(bool), typeof(DataGridAppend), new PropertyMetadata(true));
        public bool EnableSmoothScrolling
        {
            get { return (bool)GetValue(EnableSmoothScrollingProperty); }
            set { SetValue(EnableSmoothScrollingProperty, value); }
        }
    }
}
