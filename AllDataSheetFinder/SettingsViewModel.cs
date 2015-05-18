using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Controls;
using MVVMUtils;
using AllDataSheetFinder.Validation;
using System.IO;
using AllDataSheetFinder.Controls;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Diagnostics;

namespace AllDataSheetFinder
{
    public class SettingsViewModel : ObservableObject
    {
        public class LanguagePair
        {
            public string Name { get; set; }
            public string DisplayName
            {
                get
                {
                    if (string.IsNullOrEmpty(Name)) return Global.GetStringResource("StringDefault");

                    try
                    {
                        CultureInfo culture = new CultureInfo(Name);
                        return culture.NativeName;
                    }
                    catch
                    {
                        return Name;
                    }
                }
            }

            public LanguagePair(string name)
            {
                Name = name;
            }
        }

        public SettingsViewModel()
        {
            m_okCommand = new RelayCommand(Ok, CanOk);
            m_cancelCommand = new RelayCommand(Cancel);
            m_clearDatasheetsCacheCommand = new RelayCommand(ClearDatasheetsCache);
            m_clearImagesCacheCommand = new RelayCommand(ClearImagesCache);
            m_clearSavedDatasheetsCommand = new RelayCommand(ClearSavedDatasheets);
            m_checkUpdatesCommand = new RelayCommand(CheckUpdates);

            m_validators = new ValidatorCollection(() => m_okCommand.RaiseCanExecuteChanged());

            m_maxCacheSize = new IntegerValidator(0, 100000);
            m_maxCacheSize.ValidValue = (int)(Global.Configuration.MaxDatasheetsCacheSize / (1024 * 1024));
            m_validators.Add(m_maxCacheSize);

            LanguagePair pair = new LanguagePair(string.Empty);
            m_availableLanguages.Add(pair);
            m_selectedLanguage = pair;
            pair = new LanguagePair("en-US");
            if (Global.Configuration.Language == pair.Name) m_selectedLanguage = pair;
            m_availableLanguages.Add(pair);
            foreach (var langPath in Directory.EnumerateFiles(Global.LanguagesDirectory))
            {
                string[] tokens = Path.GetFileName(langPath).Split('.');
                if (tokens.Length < 2) continue;
                pair = new LanguagePair(tokens[1]);
                m_availableLanguages.Add(pair);
                if (pair.Name == Global.Configuration.Language) m_selectedLanguage = pair;
            }
        }

        private FileVersionInfo m_fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        public string Author
        {
            get
            {
                return m_fileVersionInfo.CompanyName;
            }
        }
        public string Version
        {
            get
            {
                return m_fileVersionInfo.ProductVersion;
            }
        }
        public string License
        {
            get
            {
                return Global.GetStringResource("StringLicense");
            }
        }

        public int CurrentDatasheetsCacheSize
        {
            get
            {
                return (int)(ComputeFilesSize(Global.AppDataPath + Path.DirectorySeparatorChar + Global.DatasheetsCacheDirectory) / (1024 * 1024));
            }
        }
        public int CurrentImagesCacheSize
        {
            get
            {
                return (int)(ComputeFilesSize(Global.AppDataPath + Path.DirectorySeparatorChar + Global.ImagesCacheDirectory) / 1024);
            }
        }
        public int CurrentSavedDatasheetsSize
        {
            get
            {
                return (int)(ComputeFilesSize(Global.AppDataPath + Path.DirectorySeparatorChar + Global.SavedDatasheetsDirectory) / (1024 * 1024));
            }
        }

        private ValidatorCollection m_validators;

        private IntegerValidator m_maxCacheSize;
        public IntegerValidator MaxCacheSize
        {
            get { return m_maxCacheSize; }
        }

        private RelayCommand m_okCommand;
        public ICommand OkCommand
        {
            get { return m_okCommand; }
        }

        private RelayCommand m_cancelCommand;
        public ICommand CancelCommand
        {
            get { return m_cancelCommand; }
        }

        private RelayCommand m_clearDatasheetsCacheCommand;
        public ICommand ClearDatasheetsCacheCommand
        {
            get { return m_clearDatasheetsCacheCommand; }
        }

        private RelayCommand m_clearImagesCacheCommand;
        public ICommand ClearImagesCacheCommand
        {
            get { return m_clearImagesCacheCommand; }
        }

        private RelayCommand m_clearSavedDatasheetsCommand;
        public ICommand ClearSavedDatasheetsCommand
        {
            get { return m_clearSavedDatasheetsCommand; }
        }

        private RelayCommand m_checkUpdatesCommand;
        public ICommand CheckUpdatesCommand
        {
            get { return m_checkUpdatesCommand; }
        }

        private ObservableCollection<LanguagePair> m_availableLanguages = new ObservableCollection<LanguagePair>();
        public ObservableCollection<LanguagePair> AvailableLanguages
        {
            get { return m_availableLanguages; }
        }

        private LanguagePair m_selectedLanguage;
        public LanguagePair SelectedLanguage
        {
            get { return m_selectedLanguage; }
            set { m_selectedLanguage = value; RaisePropertyChanged("SelectedLanguage"); }
        }

        private void Ok(object param)
        {
            Global.Configuration.MaxDatasheetsCacheSize = m_maxCacheSize.ValidValue * 1024 * 1024;
            if (Global.Configuration.Language != m_selectedLanguage.Name)
            {
                Global.Configuration.Language = m_selectedLanguage.Name;
                Global.ApplyLanguage();
                Global.MessageBox(this, Global.GetStringResource("StringLanguageChangeRestart"), MessageBoxExPredefinedButtons.Ok);
            }
            Global.SaveConfiguration();

            Global.Dialogs.Close(this);
        }

        private bool CanOk(object param)
        {
            return m_validators.IsValidAll;
        }
        private void Cancel(object param)
        {
            Global.Dialogs.Close(this);
        }

        private void ClearDatasheetsCache(object param)
        {
            if (Global.MessageBox(this, Global.GetStringResource("StringDoYouWantClearDatasheetsCache"), MessageBoxExPredefinedButtons.YesNo) != MessageBoxExButton.Yes) return;

            foreach (string file in Directory.EnumerateFiles(Global.AppDataPath + Path.DirectorySeparatorChar + Global.DatasheetsCacheDirectory))
            {
                File.Delete(file);
            }

            foreach (var item in Global.Main.SearchResults) item.CheckState();

            RaisePropertyChanged("CurrentDatasheetsCacheSize");
        }

        private void ClearImagesCache(object param)
        {
            if (Global.MessageBox(this, Global.GetStringResource("StringDoYouWantClearImagesCache"), MessageBoxExPredefinedButtons.YesNo) != MessageBoxExButton.Yes) return;

            foreach (string file in Directory.EnumerateFiles(Global.AppDataPath + Path.DirectorySeparatorChar + Global.ImagesCacheDirectory))
            {
                File.Delete(file);
            }

            RaisePropertyChanged("CurrentImagesCacheSize");
        }

        private void ClearSavedDatasheets(object param)
        {
            if (Global.MessageBox(this, Global.GetStringResource("StringDoYouWantClearSavedDatasheets"), MessageBoxExPredefinedButtons.YesNo) != MessageBoxExButton.Yes) return;

            foreach (string file in Directory.EnumerateFiles(Global.AppDataPath + Path.DirectorySeparatorChar + Global.SavedDatasheetsDirectory))
            {
                File.Delete(file);
            }

            Global.Main.SavedParts.Clear();
            if (Global.Main.IsFavouritesMode)
            {
                Global.Main.SearchResults.Clear();
                Global.Main.FilteredResults.Refresh();
            }
            else
            {
                foreach (var item in Global.Main.SearchResults) item.CheckState();
            }

            Global.SaveSavedParts();

            RaisePropertyChanged("CurrentSavedDatasheetsSize");
        }

        private void CheckUpdates(object param)
        {
            Global.Main.CheckForUpdates();
        }

        private long ComputeFilesSize(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            IEnumerable<FileInfo> files = dir.EnumerateFiles();

            long size = 0;
            foreach (var item in files)
            {
                size += item.Length;
            }

            return size;
        }
    }
}
