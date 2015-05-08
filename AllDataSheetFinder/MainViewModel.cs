using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Data;
using MVVMUtils;
using AllDataSheetFinder.Controls;

namespace AllDataSheetFinder
{
    public sealed class MainViewModel : ObservableObject
    {
        public MainViewModel()
        {
            m_searchCommand = new RelayCommand(Search, CanSearch);
            m_openPdfCommand = new RelayCommand(OpenPdf);
            m_loadMoreResultCommand = new RelayCommand(LoadMoreResults, CanLoadMoreResults);
            m_addToFavouritesCommand = new RelayCommand(AddToFavourites);
            m_saveFavouritesCommand = new RelayCommand(SaveFavourites);
            m_showFavouritesCommand = new RelayCommand(ShowFavourites, CanShowFavourites);
            m_settingsCommand = new RelayCommand(Settings);
            m_requestMoreInfoCommand = new RelayCommand(RequestMoreInfo);

            m_filteredResults = CollectionViewSource.GetDefaultView(m_searchResults);
            m_filteredResults.Filter = (x) =>
            {
                if (!IsFavouritesMode) return true;
                return ((PartHandler)x).Part.Name.ToUpper().Contains(m_searchField.ToUpper());
            };
        }

        private int m_openingCount = 0;
        private AllDataSheetSearchContext m_searchContext;

        private bool m_searching = false;
        public bool Searching
        {
            get { return m_searching; }
            set
            {
                m_searching = value;
                RaisePropertyChanged("Searching");
                m_searchCommand.RaiseCanExecuteChanged();
                m_loadMoreResultCommand.RaiseCanExecuteChanged();
                m_showFavouritesCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged("LoadMoreVisible");
            }
        }

        private string m_searchField;
        public string SearchField
        {
            get { return m_searchField; }
            set
            {
                m_searchField = value;
                RaisePropertyChanged("SearchField");
                m_searchCommand.RaiseCanExecuteChanged();

                if (IsFavouritesMode) m_filteredResults.Refresh();
            }
        }

        private ObservableCollection<PartHandler> m_searchResults = new ObservableCollection<PartHandler>();
        public ObservableCollection<PartHandler> SearchResults
        {
            get { return m_searchResults; }
        }

        private ICollectionView m_filteredResults;
        public ICollectionView FilteredResults
        {
            get { return m_filteredResults; }
        }

        private PartHandler m_selectedResult;
        public PartHandler SelectedResult
        {
            get { return m_selectedResult; }
            set { m_selectedResult = value; RaisePropertyChanged("SelectedResult"); }
        }

        private bool m_isFavouritesMode = false;
        public bool IsFavouritesMode
        {
            get { return m_isFavouritesMode; }
            set { m_isFavouritesMode = value; RaisePropertyChanged("IsFavouritesMode"); RaisePropertyChanged("LoadMoreVisible"); m_loadMoreResultCommand.RaiseCanExecuteChanged(); }
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

        private RelayCommand m_addToFavouritesCommand;
        public ICommand AddToFavouritesCommand
        {
            get { return m_addToFavouritesCommand; }
        }

        private RelayCommand m_saveFavouritesCommand;
        public ICommand SaveFavouritesCommand
        {
            get { return m_saveFavouritesCommand; }
        }

        private RelayCommand m_showFavouritesCommand;
        public ICommand ShowFavouritesCommand
        {
            get { return m_showFavouritesCommand; }
        }

        private RelayCommand m_settingsCommand;
        public ICommand SettingsCommand
        {
            get { return m_settingsCommand; }
        }

        private RelayCommand m_requestMoreInfoCommand;
        public ICommand RequestMoreInfoCommand
        {
            get { return m_requestMoreInfoCommand; }
        }

        public bool LoadMoreVisible
        {
            get { return !IsFavouritesMode && m_searchContext != null && m_searchContext.CanLoadMore; }
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
            m_searchResults.Clear();
            m_searchContext = null;
            IsFavouritesMode = false;
            Searching = true;
            m_filteredResults.Refresh();
            Mouse.OverrideCursor = Cursors.AppStarting;

            try
            {
                AllDataSheetSearchResult result = await AllDataSheetPart.SearchAsync(m_searchField);
                m_searchContext = result.SearchContext;
                AddResults(result.Parts);
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringSearchError"), MessageBoxExPredefinedButtons.Ok);
            }

            if(m_openingCount <= 0) Mouse.OverrideCursor = null;
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
                Global.MessageBox(this, Global.GetStringResource("StringDownloadError"), MessageBoxExPredefinedButtons.Ok);
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
                Global.MessageBox(this, Global.GetStringResource("StringSearchError"), MessageBoxExPredefinedButtons.Ok);
            }

            Mouse.OverrideCursor = null;
            Searching = false;
        }
        private bool CanLoadMoreResults(object param)
        {
            return !Searching && !IsFavouritesMode && m_searchContext != null && m_searchContext.CanLoadMore;
        }

        private async void AddToFavourites(object param)
        {
            if (m_selectedResult == null) return;

            if (m_selectedResult.State == PartDatasheetState.Saved)
            {
                if (Global.MessageBox(this, Global.GetStringResource("StringDoYouWantToRemoveFromFavourites"), MessageBoxExPredefinedButtons.YesNo) != MessageBoxExButton.Yes) return;
                m_selectedResult.RemovePdf();
                if (IsFavouritesMode)
                {
                    SavedPart toRemove = null;
                    foreach (var item in Global.SavedParts)
                    {
                        string code = AllDataSheetPart.BuildCodeFromLink(item.DatasheetSiteLink, item.Name, item.Manufacturer, item.DatasheetSiteLink.GetHashCode().ToString());
                        if (code == m_selectedResult.Part.Code)
                        {
                            toRemove = item;
                            break;
                        }
                    }
                    if (toRemove != null) Global.SavedParts.Remove(toRemove);
                    m_searchResults.Remove(m_selectedResult);
                }
                return;
            }

            try
            {
                m_openingCount++;
                Mouse.OverrideCursor = Cursors.AppStarting;
                await m_selectedResult.SavePdf();
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringDownloadError"), MessageBoxExPredefinedButtons.Ok);
            }
            finally
            {
                m_openingCount--;
                if (m_openingCount <= 0) Mouse.OverrideCursor = null;
            }
        }

        private void SaveFavourites(object param)
        {
            Global.SaveSavedParts();
        }

        private void ShowFavourites(object param)
        {
            SearchField = string.Empty;
            IsFavouritesMode = true;

            Global.SavedParts.Sort((x, y) => y.LastUseDate.CompareTo(x.LastUseDate));
            
            m_searchResults.Clear();
            foreach (var item in Global.SavedParts)
            {
                PartHandler handler = item.ToPartHandler();
                handler.LoadImage();
                m_searchResults.Add(handler);
            }
        }
        private bool CanShowFavourites(object param)
        {
            return !Searching;
        }

        private void Settings(object param)
        {
            SettingsViewModel dialogViewModel = new SettingsViewModel();
            Global.Dialogs.ShowDialog(this, dialogViewModel);
        }

        private async void RequestMoreInfo(object param)
        {
            if (m_selectedResult == null) return;
            if (m_selectedResult.MoreInfoState == PartMoreInfoState.Downloading) return;
            else if (m_selectedResult.MoreInfoState == PartMoreInfoState.Available)
            {
                if (Global.MessageBox(this, Global.GetStringResource("StringDoYouWantUpdateMoreInfo"), MessageBoxExPredefinedButtons.YesNo) != MessageBoxExButton.Yes) return;
            }

            try
            {
                m_openingCount++;
                Mouse.OverrideCursor = Cursors.AppStarting;
                await m_selectedResult.RequestMoreInfo();
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringMoreInfoError"), MessageBoxExPredefinedButtons.Ok);
            }
            finally
            {
                m_openingCount--;
                if (m_openingCount <= 0) Mouse.OverrideCursor = null;
            }
        }
    }
}
