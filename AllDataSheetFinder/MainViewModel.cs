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
        }

        private int m_downloadingCount = 0;

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

        private RelayCommand m_openPdfCommand;
        public ICommand OpenPdfCommand
        {
            get { return m_openPdfCommand; }
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

        private async void OpenPdf(object param)
        {
            if (m_selectedResult == null) return;
            string code = m_selectedResult.Code;
            string pdfPath = Global.BuildSavedDatasheetPath(code);
            if (File.Exists(pdfPath))
            {
                Process.Start(pdfPath);
                return;
            }

            pdfPath = Global.BuildCachedDatasheetPath(code);
            if (File.Exists(pdfPath))
            {
                Process.Start(pdfPath);
                return;
            }
            else
            {
                Stream stream = null;
                try
                {
                    m_downloadingCount++;
                    Mouse.OverrideCursor = Cursors.AppStarting;

                    stream = await m_selectedResult.GetDatasheetStreamAsync();
                    await Task.Run(() =>
                        {
                            using (FileStream file = new FileStream(pdfPath, FileMode.OpenOrCreate))
                            {
                                byte[] buffer = new byte[4096];
                                int len;
                                while ((len = stream.Read(buffer, 0, buffer.Length)) > 0) file.Write(buffer, 0, len);
                            }
                        });
                    Process.Start(pdfPath);
                }
                catch
                {
                    Global.MessageBox(this, Global.GetStringResource("StringDownloadError"), MessageBoxSuperPredefinedButtons.OK);
                }
                finally
                {
                    if (stream != null) stream.Close();
                    m_downloadingCount--;
                    if (m_downloadingCount <= 0) Mouse.OverrideCursor = null;
                }
            }
        }
    }
}
