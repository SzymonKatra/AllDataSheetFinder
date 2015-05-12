using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Globalization;

namespace AllDataSheetFinder.Validation
{
    public abstract class ValidationRuleWithResult<T> : ValidationRule
    {
        private T m_validResult;
        public T ValidResult
        {
            get { return m_validResult; }
            protected set { m_validResult = value; }
        }

        public static string ProcessValidation(ValidationRuleWithResult<T> rule, object inputValue, ref T outputValue, out bool isValid)
        {
            ValidationResult result = rule.Validate(inputValue, System.Globalization.CultureInfo.CurrentCulture);

            isValid = result.IsValid;
            if (result.IsValid) outputValue = rule.ValidResult;

            string res = result.ErrorContent as string;

            return (res == null ? null : res);
        }
    }
}
