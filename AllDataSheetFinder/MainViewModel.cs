using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MVVMUtils;
using AllDataSheetFinder.SiteParser;

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

        private void Search(object param)
        {
            //string x = SiteActions.Search(m_searchField);
            //Console.WriteLine(x);
            var x = SiteActions.Search(m_searchField);
            Console.WriteLine(x.Count);
            foreach (var item in x)
            {
                Console.WriteLine(item.PartName + " - " + item.PartDescription + " - " + item.Manufacturer + " - " + item.ManufacturerImageLink + " - " + item.DatasheetSiteLink);
            }
        }
        private bool CanSearch(object param)
        {
            return !string.IsNullOrWhiteSpace(m_searchField);
        }
    }
}
