using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AllDataSheetFinder.Validation
{
    public class IntegerValidator : Validator<int>
    {
        public IntegerValidator()
        {
        }
        public IntegerValidator(int min, int max)
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

        protected override ValidatorResult Validate()
        {
            if (Input == null) return ValidatorResult.CreateInvalid(Global.GetStringResource("StringInnerError"));

            int result;
            if (!int.TryParse(Input, out result)) return ValidatorResult.CreateInvalid(Global.GetStringResource("StringIllegalCharacters"));

            if (result < m_min || result > m_max) return ValidatorResult.CreateInvalid(string.Format(Global.GetStringResource("StringFormatMustBeInRange"), m_min, m_max));

            return ValidatorResult.CreateValid(result);
        }
    }
}
