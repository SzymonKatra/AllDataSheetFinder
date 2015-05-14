using MVVMUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder.Validation
{
    public abstract class Validator<T> : ObservableObject, IDataErrorInfo, IValidator
    {
        protected struct ValidatorResult<T>
        {
            public string Error;
            public bool IsValid
            {
                get { return string.IsNullOrEmpty(Error); }
            }
            public T ValidResult;

            public static ValidatorResult<T> CreateValid(T result)
            {
                ValidatorResult<T> x = new ValidatorResult<T>();
                x.Error = string.Empty;
                x.ValidResult = result;
                return x;
            }
            public static ValidatorResult<T> CreateInvalid(string error)
            {
                ValidatorResult<T> x = new ValidatorResult<T>();
                x.Error = error;
                return x;
            }
        }

        private bool m_isValid;
        public bool IsValid
        {
            get { return m_isValid; }
        }

        private string m_error;
        public string Error
        {
            get { return m_error; }
        }

        private T m_result;
        public T Result
        {
            get { return m_result; }
        }

        private string m_input;
        public string Input
        {
            get { return m_input; }
            set { m_input = value; RaisePropertyChanged("Input"); }
        }

        public event EventHandler IsValidChanged;

        public void ForceValidate()
        {
            ValidatorResult<T> result = ProcessValidation();

            bool oldIsValid = m_isValid;

            if (result.IsValid)
            {
                m_isValid = true;
                m_error = string.Empty;
                m_result = result.ValidResult;
            }
            else
            {
                m_isValid = false;
                m_error = result.Error;
            }

            if (oldIsValid != m_isValid) RaiseIsValidChanged();
        }

        protected abstract ValidatorResult<T> ProcessValidation();

        protected void RaiseIsValidChanged()
        {
            EventHandler handler = IsValidChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public string this[string columnName]
        {
            get
            {
                if (columnName != "Input") return string.Empty;
                ForceValidate();
                return Error;
            }
        }
    }
}
