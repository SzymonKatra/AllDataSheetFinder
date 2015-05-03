using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using System.Collections.ObjectModel;
using MVVMUtils;

namespace AllDataSheetFinder
{
    public sealed class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            m_searchCommand = new RelayCommand(Search, CanSearch);
        }

        private bool m_searching = false;
        public bool Searching
        {
            get { return m_searching; }
            set { m_searching = value; RaisePropertyChanged("Searching"); m_searchCommand.RaiseCanExecuteChanged(); }
        }

        private string m_searchField;
        public string SearchField
        {
            get { return m_searchField; }
            set { m_searchField = value; RaisePropertyChanged("SearchField"); m_searchCommand.RaiseCanExecuteChanged(); }
        }

        private ObservableCollection<AllDataSheetPart> m_searchResults = new ObservableCollection<AllDataSheetPart>();
        public ObservableCollection<AllDataSheetPart> SearchResults
        {
            get { return m_searchResults; }
        }

        private AllDataSheetPart m_selectedResult;
        public AllDataSheetPart SelectedResult
        {
            get { return m_selectedResult; }
            set { m_selectedResult = value; RaisePropertyChanged("SelectedResult"); }
        }

        private RelayCommand m_searchCommand;
        public ICommand SearchCommand
        {
            get { return m_searchCommand; }
        }

        private async void Search(object param)
        {
            Searching = true;

            try
            {
                List<AllDataSheetPart> results = await AllDataSheetPart.SearchAsync(m_searchField);
                m_searchResults.Clear();
                foreach (var item in results) m_searchResults.Add(item);
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringSearchError"), MessageBoxSuperPredefinedButtons.OK);
            }

            Searching = false;
        }
        private bool CanSearch(object param)
        {
            return !string.IsNullOrWhiteSpace(m_searchField) && !m_searching;
        }
    }
}
