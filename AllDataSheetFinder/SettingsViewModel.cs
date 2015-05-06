﻿using System;
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

namespace AllDataSheetFinder
{
    public class SettingsViewModel : ObservableObject, IDataErrorInfo
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

            m_maxCacheSize = (Global.Configuration.MaxDatasheetsCacheSize / (1024 * 1024)).ToString();

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

        private IntegerRule m_maxCacheSizeRule = new IntegerRule(0, 100000);
        private int m_maxCacheSizeValue;
        private bool m_maxCacheSizeIsValid = false;
        private string m_maxCacheSize;
        public string MaxCacheSize
        {
            get { return m_maxCacheSize; }
            set { m_maxCacheSize = value; RaisePropertyChanged("MaxCacheSize"); }
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
            Global.Configuration.MaxDatasheetsCacheSize = m_maxCacheSizeValue * 1024 * 1024;
            if (Global.Configuration.Language != m_selectedLanguage.Name)
            {
                Global.Configuration.Language = m_selectedLanguage.Name;
                Global.ApplyLanguage();
            }
            Global.SaveConfiguration();

            Global.Dialogs.Close(this);
        }

        private bool CanOk(object param)
        {
            return m_maxCacheSizeIsValid;
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

            Global.SavedParts.Clear();
            Global.SaveSavedParts();

            RaisePropertyChanged("CurrentSavedDatasheetsSize");
        }

        public string Error
        {
            get { return string.Empty; }
        }
        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case "MaxCacheSize":
                        string r = ProcessValidation(m_maxCacheSizeRule, m_maxCacheSize, ref m_maxCacheSizeValue, out m_maxCacheSizeIsValid);
                        m_okCommand.RaiseCanExecuteChanged();
                        return r;
                }

                return string.Empty;
            }
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

        private string ProcessValidation<T>(ValidationRuleWithResult<T> rule, object inputValue, ref T outputValue, out bool isValid)
        {
            ValidationResult result = rule.Validate(inputValue, System.Globalization.CultureInfo.CurrentCulture);

            isValid = result.IsValid;
            if (result.IsValid) outputValue = rule.ValidResult;

            string res = result.ErrorContent as string;

            return (res == null ? null : res);
        }
    }
}
