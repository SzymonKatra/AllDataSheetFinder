using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using MVVMUtils;

namespace AllDataSheetFinder
{
    public class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            m_searchCommand = new RelayCommand(Search, CanSearch);
        }

        private string m_searchField;
        public string SearchField
        {
            get { return m_searchField; }
            set { m_searchField = value; RaisePropertyChanged("SearchField"); m_searchCommand.RaiseCanExecuteChanged(); }
        }

        private RelayCommand m_searchCommand;
        public ICommand SearchCommand
        {
            get { return m_searchCommand; }
        }

        private async void Search(object param)
        {
            var x = await AllDataSheetPart.SearchAsync(m_searchField);
            Console.WriteLine("znaleziono");
            await Task.Run(() => 
            {
                using (Stream stream = x.ElementAt(0).DownloadDatasheet())
                {
                    using (FileStream file = new FileStream(@"C:\Users\Szymon\Desktop\ds.pdf", FileMode.OpenOrCreate))
                    {
                        byte[] buffer = new byte[4096];
                        int len;
                        while ((len = stream.Read(buffer, 0, buffer.Length)) > 0) file.Write(buffer, 0, len);
                    }
                }
            });
            Console.WriteLine("pobrano");
            
        }
        private bool CanSearch(object param)
        {
            return !string.IsNullOrWhiteSpace(m_searchField);
        }
    }
}
