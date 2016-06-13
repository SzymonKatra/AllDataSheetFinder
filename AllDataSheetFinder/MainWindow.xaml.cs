using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
    public partial class MainWindow : Window, IDisposable
    {
        private Mutex m_oneInstanceMutex;

        public MainWindow()
        {
            if (Mutex.TryOpenExisting(Global.MutexName, out m_oneInstanceMutex))
            {
                m_oneInstanceMutex.Close();
                this.Close();
                return;
            }

            m_oneInstanceMutex = new Mutex(true, Global.MutexName);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            InitializeComponent();

            this.Width = SystemParameters.PrimaryScreenWidth * 0.6;
            this.Height = SystemParameters.PrimaryScreenHeight * 0.7;
            this.Loaded += MainWindow_Loaded;
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            Global.InitializeAll();

            MainViewModel main = new MainViewModel();
            main.NeedClose = () => this.Close();
            Global.Main = main;

            this.DataContext = main;

            Global.Dialogs.Register(this, main);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("====================");
                sb.AppendLine(DateTime.Now.ToString());
                sb.AppendLine("====================");
                sb.AppendLine();
                sb.AppendLine("Is terminating: " + e.IsTerminating);
                sb.AppendLine();
                sb.AppendLine(e.ExceptionObject.ToString());
                string error = sb.ToString();

                using (StreamWriter writer = new StreamWriter(Global.ErrorLogFileName, true))
                {
                    writer.Write(error);
                }
            }
            catch { }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBoxSearch.Focus();
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

        #region IDisposable
        private bool m_disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    // managed
                    m_oneInstanceMutex.Dispose();
                }

                // unmanaged

                m_disposed = true;
            }
        }
        ~MainWindow()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
