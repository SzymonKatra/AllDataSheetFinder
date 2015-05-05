using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AllDataSheetFinder.Validation
{
    public class IntegerRule : ValidationRuleWithResult<int>
    {
        public IntegerRule()
        {
        }
        public IntegerRule(int min, int max)
        {
            m_min = min;
            m_max = max;
        }

        private int m_min = int.MinValue;
        public int Min
        {
            get { return m_min; }
            set { m_min = value; }
        }

        private int m_max = int.MaxValue;
        public int Max
        {
            get { return m_max; }
            set { m_max = value; }
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            string str = value as string;
            if (str == null) return new ValidationResult(false, Global.GetStringResource("StringInnerError"));

            int result;
            if (!int.TryParse(str, out result)) return new ValidationResult(false, Global.GetStringResource("StringIllegalCharacters"));

            if (result < m_min || result > m_max) return new ValidationResult(false, string.Format(Global.GetStringResource("StringFormatMustBeInRange"), m_min, m_max));

            ValidResult = result;

            return new ValidationResult(true, null);
        }
    }
}
