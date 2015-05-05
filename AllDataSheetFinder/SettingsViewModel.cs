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

namespace AllDataSheetFinder
{
    public class SettingsViewModel : ObservableObject, IDataErrorInfo
    {
        public SettingsViewModel()
        {
            m_okCommand = new RelayCommand(Ok, CanOk);
            m_cancelCommand = new RelayCommand(Cancel);

            m_maxCacheSize = (Global.Configuration.MaxDatasheetsCacheSize / (1024 * 1024)).ToString();
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

        private void Ok(object param)
        {
            Global.Configuration.MaxDatasheetsCacheSize = m_maxCacheSizeValue * 1024 * 1024;
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
