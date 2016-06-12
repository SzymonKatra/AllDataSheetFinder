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
using MVVMUtils.Collections;
using AllDataSheetFinder.Controls;
using Microsoft.Win32;
using System.Net;
using System.Xml.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Shell;

namespace AllDataSheetFinder
{
    public delegate void NeedCloseDelegate();

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
            m_addCustomCommand = new RelayCommand(AddCustom);

            m_filteredResults = CollectionViewSource.GetDefaultView(m_searchResults);
            m_filteredResults.Filter = TagsFilter;
            m_filteredResults.Refresh();

            m_savedParts = new SynchronizedObservableCollection<PartViewModel, Part>(Global.SavedParts, (m) => new PartViewModel(m));

            CheckForUpdates();
            if (Global.Configuration.FavouritesOnStart) ShowFavourites(null);
        }

        public NeedCloseDelegate NeedClose;

        private bool m_checkingUpdates = false;
        private int m_actionsCount;
        public int ActionsCount
        {
            get { return m_actionsCount; }
            set
            {
                m_actionsCount = value;
                Mouse.OverrideCursor = (m_actionsCount <= 0 ? null : Cursors.AppStarting);
                RaisePropertyChanged(nameof(ActionsCount));
                RaisePropertyChanged(nameof(TaskbarProgressState));
            }
        }
        public TaskbarItemProgressState TaskbarProgressState
        {
            get { return (m_actionsCount <= 0 ? TaskbarItemProgressState.None : TaskbarItemProgressState.Indeterminate); }
        }
        private AllDataSheetSearchContext m_searchContext;

        private bool m_searching = false;
        public bool Searching
        {
            get { return m_searching; }
            set
            {
                m_searching = value;
                RaisePropertyChanged(nameof(Searching));
                m_searchCommand.RaiseCanExecuteChanged();
                m_loadMoreResultCommand.RaiseCanExecuteChanged();
                m_showFavouritesCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(LoadMoreVisible));
            }
        }

        private string m_searchField;
        public string SearchField
        {
            get { return m_searchField; }
            set
            {
                m_searchField = value;
                RaisePropertyChanged(nameof(SearchField));
                m_searchCommand.RaiseCanExecuteChanged();

                if (IsFavouritesMode) m_filteredResults.Refresh();
            }
        }

        private ObservableCollection<PartViewModel> m_searchResults = new ObservableCollection<PartViewModel>();
        public ObservableCollection<PartViewModel> SearchResults
        {
            get { return m_searchResults; }
        }

        private SynchronizedObservableCollection<PartViewModel, Part> m_savedParts;
        public SynchronizedObservableCollection<PartViewModel, Part> SavedParts
        {
            get { return m_savedParts; }
        }

        private SortDescription m_sortDescription = new SortDescription("LastUseDate", ListSortDirection.Descending);
        private ICollectionView m_filteredResults;
        public ICollectionView FilteredResults
        {
            get { return m_filteredResults; }
        }

        private PartViewModel m_selectedResult;
        public PartViewModel SelectedResult
        {
            get { return m_selectedResult; }
            set { m_selectedResult = value; RaisePropertyChanged(nameof(SelectedResult)); }
        }

        private bool m_isFavouritesMode = false;
        public bool IsFavouritesMode
        {
            get { return m_isFavouritesMode; }
            set
            {
                m_isFavouritesMode = value;
                RaisePropertyChanged(nameof(IsFavouritesMode));
                RaisePropertyChanged(nameof(LoadMoreVisible));
                m_loadMoreResultCommand.RaiseCanExecuteChanged();
            }
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

        private RelayCommand m_addCustomCommand;
        public ICommand AddCustomCommand
        {
            get { return m_addCustomCommand; }
        }

        public bool LoadMoreVisible
        {
            get { return !IsFavouritesMode && m_searchContext != null && m_searchContext.CanLoadMore; }
        }

        private void AddResults(List<AllDataSheetPart> results)
        {
            foreach (var item in results)
            {
                PartViewModel viewModel = PartViewModel.FromAllDataSheetPart(item);
                PartViewModel found = m_savedParts.FirstOrDefault(x => x.Code == viewModel.Code);
                if (found != null) viewModel = found;
                PartViewModel.GetDownloadingIfExists(viewModel.Code, ref viewModel);
                if (viewModel.Context.OnlyContext) viewModel.Context = item;
                viewModel.LoadImage();
                m_searchResults.Add(viewModel);
            }
        }
        
        private async void Search(object param)
        {
            m_searchResults.Clear();
            m_searchContext = null;
            IsFavouritesMode = false;
            Searching = true;
            m_filteredResults.SortDescriptions.Clear();
            m_filteredResults.Refresh();

            try
            {
                ActionsCount++;
                AllDataSheetSearchResult result = await AllDataSheetPart.SearchAsync(m_searchField);
                m_searchContext = result.SearchContext;
                AddResults(result.Parts);
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringSearchError"), MessageBoxExPredefinedButtons.Ok);
            }
            finally
            {
                ActionsCount--;
            }

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
                ActionsCount++;
                await m_selectedResult.OpenPdf();
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringDownloadError"), MessageBoxExPredefinedButtons.Ok);
            }
            finally
            {
                ActionsCount--;
            }
        }

        private async void LoadMoreResults(object param)
        {
            Searching = true;

            try
            {
                ActionsCount++;
                AllDataSheetSearchResult result = await AllDataSheetPart.SearchAsync(m_searchContext);
                m_searchContext = result.SearchContext;
                AddResults(result.Parts);
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringSearchError"), MessageBoxExPredefinedButtons.Ok);
            }
            finally
            {
                ActionsCount--;
            }

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
                m_savedParts.Remove(m_selectedResult);
                if (IsFavouritesMode)
                {
                    m_searchResults.Remove(m_selectedResult);
                    Global.SaveSavedParts();
                }
                return;
            }

            try
            {
                ActionsCount++;
                PartViewModel part = m_selectedResult;
                await part.SavePdf();
                m_savedParts.Add(part);
                Global.SaveSavedParts();
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringDownloadError"), MessageBoxExPredefinedButtons.Ok);
            }
            finally
            {
                ActionsCount--;
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
            m_filteredResults.SortDescriptions.Add(m_sortDescription);
            m_filteredResults.Refresh();
            
            m_searchResults.Clear();
            foreach (var item in m_savedParts)
            {
                item.LoadImage();
                m_searchResults.Add(item);
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
                m_selectedResult.PushCopy();
                EditPartViewModel dialogViewModel = new EditPartViewModel(m_selectedResult);
                Global.Dialogs.ShowDialog(this, dialogViewModel);
                if (dialogViewModel.Result == EditPartViewModel.EditPartResult.Ok) m_selectedResult.PopCopy(WorkingCopyResult.Apply);
                else if (dialogViewModel.Result == EditPartViewModel.EditPartResult.Cancel) m_selectedResult.PopCopy(WorkingCopyResult.Restore);
                return;
            }

            try
            {
                ActionsCount++;
                await m_selectedResult.RequestMoreInfo();
            }
            catch
            {
                Global.MessageBox(this, Global.GetStringResource("StringMoreInfoError"), MessageBoxExPredefinedButtons.Ok);
            }
            finally
            {
                ActionsCount--;
            }
        }

        private void AddCustom(object param)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = Global.PdfFilter;
            openFileDialog.Multiselect = true;
            openFileDialog.ShowDialog(Global.Dialogs.GetWindow(this));
            foreach (var fileName in openFileDialog.FileNames)
            {
                if (string.IsNullOrWhiteSpace(fileName) || !File.Exists(fileName)) continue;

                bool add = true;
                PartViewModel part = null;

                if (openFileDialog.FileNames.Length == 1) // if there is only one pdf to add, open window to edit properties of it
                {
                    EditPartViewModel dialogViewModel = new EditPartViewModel(openFileDialog.FileName);
                    Global.Dialogs.ShowDialog(this, dialogViewModel);
                    if (dialogViewModel.Result != EditPartViewModel.EditPartResult.Ok)
                        add = false;
                    else
                        part = dialogViewModel.Part;
                }
                else // if more then just add it
                {
                    part = PartViewModel.MakeCustom(fileName);
                    File.Copy(fileName, part.CustomPath);
                }

                if (add)
                {
                    m_searchResults.Add(part);
                    m_savedParts.Add(part);
                    part.LastUseDate = DateTime.MinValue;

                    ActionDialogViewModel actionDialogViewModel = new ActionDialogViewModel(part.ComputePagesCount(), Global.GetStringResource("StringCountingPages"));
                    Global.Dialogs.ShowDialog(this, actionDialogViewModel);
                }
            }

            m_filteredResults.Refresh();
        }

        public async void CheckForUpdates(bool raisedManually = false)
        {
            if (m_checkingUpdates) return;

            m_checkingUpdates = true;
            bool newVersion = false;

            NewVersionInfo? info = null;
            try
            {
                info = await NewVersionInfo.RequestInfoAsync(Global.UpdateVersionLink);
            }
            catch
            {
                try
                {
                    info = await NewVersionInfo.RequestInfoAsync(Global.AdditionalUpdateVersionLink);
                }
                catch
                {
                    if (raisedManually) Global.MessageBox(this, Global.GetStringResource("StringCheckUpdatesError"), MessageBoxExPredefinedButtons.Ok);
                }
            }

            Version currentVersion = Version.Parse(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion);
            if (info != null && info.Value.Version > currentVersion) newVersion = true;

            if (raisedManually && !newVersion) Global.MessageBox(this, Global.GetStringResource("StringNoUpdatesFound"), MessageBoxExPredefinedButtons.Ok);
            if (newVersion && Global.MessageBox(this, Global.GetStringResource("StringUpdateAvailable"), MessageBoxExPredefinedButtons.YesNo) == MessageBoxExButton.Yes)
            {
                UpdateViewModel dialogViewModel = new UpdateViewModel(info.Value.Link);
                Global.Dialogs.ShowDialog(this, dialogViewModel);
                if (!dialogViewModel.DownloadSuccessful) Global.MessageBox(this, Global.GetStringResource("StringUpdateDownloadError"), MessageBoxExPredefinedButtons.Ok);
                else if (!dialogViewModel.ExtractSuccessful) Global.MessageBox(this, Global.GetStringResource("StringUpdateExtractError"), MessageBoxExPredefinedButtons.Ok);

                if (dialogViewModel.DownloadSuccessful && dialogViewModel.ExtractSuccessful)
                {
                    string basePath = Global.AppDataPath + Path.DirectorySeparatorChar + Global.UpdateExtractDirectory;
                    string appDir = AppDomain.CurrentDomain.BaseDirectory;
                    while (appDir.EndsWith("\\")) appDir = appDir.Remove(appDir.Length - 1);
                    Process.Start($"{basePath}{Path.DirectorySeparatorChar}{info.Value.Execute}", $"\"{basePath}{Path.DirectorySeparatorChar}{info.Value.Files}\" \"{appDir}\"");
                    NeedClose();
                }
            }

            m_checkingUpdates = false;
        }

        private bool TagsFilter(object x)
        {
            if (!IsFavouritesMode) return true;

            PartViewModel part = (PartViewModel)x;
            string[] tokens = m_searchField.ToUpper().Split(' ');
            string upperName = (part.Name == null ? string.Empty : part.Name.ToUpper());

            foreach (var item in tokens)
            {
                var result = part.Tags.FirstOrDefault(tag => tag.Value.ToUpper().StartsWith(item));
                if (result == null)
                {
                    if (!upperName.Contains(item)) return false;
                }
            }

            return true;
        }
    }
}
