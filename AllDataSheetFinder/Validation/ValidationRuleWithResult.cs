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
    }
}
