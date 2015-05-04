using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MVVMUtils;

namespace AllDataSheetFinder
{
    public sealed class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            m_searchCommand = new RelayCommand(Search, CanSearch);
            m_openPdfCommand = new RelayCommand(OpenPdf);
            m_loadMoreResultCommand = new RelayCommand(LoadMoreResults, CanLoadMoreResult);
        }

        private int m_openingCount = 0;
        private AllDataSheetSearchContext m_searchContext;

        private bool m_searching = false;
        public bool Searching
        {
            get { return m_searching; }
            set { m_searching = value; RaisePropertyChanged("Searching"); m_searchCommand.RaiseCanExecuteChanged(); m_loadMoreResultCommand.RaiseCanExecuteChanged(); }
        }

        private string m_searchField;
        public string SearchField
        {
            get { return m_searchField; }
            set { m_searchField = value; RaisePropertyChanged("SearchField"); m_searchCommand.RaiseCanExecuteChanged(); }
        }

        private ObservableCollection<PartHandler> m_searchResults = new ObservableCollection<PartHandler>();
        public ObservableCollection<PartHandler> SearchResults
        {
            get { return m_searchResults; }
        }

        private PartHandler m_selectedResult;
        public PartHandler SelectedResult
        {
            get { return m_selectedResult; }
            set { m_selectedResult = value; RaisePropertyChanged("SelectedResult"); }
        }

        private RelayCommand m_searchCommand;
        public ICommand SearchCommand
        {
            get { return m_searchCommand; }
        }

        private RelayCommand m_openPdfCommand;
        public ICommand OpenPdfCommand
        {
            get { return m_openPdfCommand; }
        }

        private RelayCommand m_loadMoreResultCommand;
        public ICommand LoadMoreResultsCommand
        {
            get { return m_loadMoreResultCommand; }
        }

        private void AddResults(List<AllDataSheetPart> results)
        {
            foreach (var item in results)
            {
                PartHandler handler = new PartHandler(item);
                handler.LoadImage();
                m_searchResults.Add(handler);
            }
        }

        private async void Search(object param)
        {
            Searching = true;
            Mouse.OverrideCursor = Cursors.AppStarting;

            try
            {
                AllDataSheetSearchResult result = await AllDataSheetPart.SearchAsync(m_searchField);
                m_searchContext = result.SearchContext;
                m_searchResults.Clear();
                AddResults(result.Parts);
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringSearchError"), MessageBoxSuperPredefinedButtons.OK);
            }

            Mouse.OverrideCursor = null;
            Searching = false;
        }
        private bool CanSearch(object param)
        {
            return !string.IsNullOrWhiteSpace(m_searchField) && !m_searching;
        }

        private async void OpenPdf(object param)
        {
            if (m_selectedResult == null) return;
            try
            {
                m_openingCount++;
                Mouse.OverrideCursor = Cursors.AppStarting;
                await m_selectedResult.OpenPdf();
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringDownloadError"), MessageBoxSuperPredefinedButtons.OK);
            }
            finally
            {
                m_openingCount--;
                if (m_openingCount <= 0) Mouse.OverrideCursor = null;
            }
        }

        private async void LoadMoreResults(object param)
        {
            Searching = true;
            Mouse.OverrideCursor = Cursors.AppStarting;

            try
            {
                AllDataSheetSearchResult result = await AllDataSheetPart.SearchAsync(m_searchContext);
                m_searchContext = result.SearchContext;
                AddResults(result.Parts);
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringSearchError"), MessageBoxSuperPredefinedButtons.OK);
            }

            Mouse.OverrideCursor = null;
            Searching = false;
        }
        private bool CanLoadMoreResult(object param)
        {
            return !Searching && m_searchContext != null && m_searchContext.CanLoadMore;
        }
    }
}
