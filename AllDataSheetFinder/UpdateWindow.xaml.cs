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
using System.Windows.Shapes;

namespace AllDataSheetFinder
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        public UpdateWindow(UpdateViewModel viewModel)
        {
            InitializeComponent();

            viewModel.InvokeWindow = (x) => this.Dispatcher.Invoke(x);
            this.DataContext = viewModel;

            Global.Dialogs.Register(this, viewModel);
        }
    }
}
