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
    /// <summary>
    /// Interaction logic for OkCancelControl.xaml
    /// </summary>
    public partial class OkCancelControl : UserControl
    {
        public OkCancelControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty CommandOkProperty = DependencyProperty.Register("CommandOk", typeof(ICommand), typeof(OkCancelControl), new PropertyMetadata(null));
        public ICommand CommandOk
        {
            get { return (ICommand)GetValue(CommandOkProperty); }
            set { SetValue(CommandOkProperty, value); }
        }

        public static readonly DependencyProperty CommandCancelProperty = DependencyProperty.Register("CommandCancel", typeof(ICommand), typeof(OkCancelControl), new PropertyMetadata(null));
        public ICommand CommandCancel
        {
            get { return (ICommand)GetValue(CommandCancelProperty); }
            set { SetValue(CommandCancelProperty, value); }
        }

        public static readonly DependencyProperty CommandParameterOkProperty = DependencyProperty.Register("CommandParameterOk", typeof(object), typeof(OkCancelControl), new PropertyMetadata(null));
        public object CommandParameterOk
        {
            get { return GetValue(CommandParameterOkProperty); }
            set { SetValue(CommandParameterOkProperty, value); }
        }

        public static readonly DependencyProperty CommandParameterCancelProperty = DependencyProperty.Register("CommandParameterCancel", typeof(object), typeof(OkCancelControl), new PropertyMetadata(null));
        public object CommandParameterCancel
        {
            get { return GetValue(CommandParameterCancelProperty); }
            set { SetValue(CommandParameterCancelProperty, value); }
        }
    }
}
