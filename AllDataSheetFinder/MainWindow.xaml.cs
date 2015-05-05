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

namespace AllDataSheetFinder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Width = SystemParameters.PrimaryScreenWidth * 0.6;
            this.Height = SystemParameters.PrimaryScreenHeight * 0.7;
            this.Loaded += MainWindow_Loaded;

            Global.InitializeAll();

            MainViewModel main = new MainViewModel();
            Global.Main = main;

            this.DataContext = main;

            Global.Dialogs.Register(this, main);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxSearch.Focus();
        }

        private void TextBoxSearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                MainViewModel vm = (MainViewModel)this.DataContext;
                if (vm.SearchCommand.CanExecute(null)) vm.SearchCommand.Execute(null);
            }
        }
    }
}
