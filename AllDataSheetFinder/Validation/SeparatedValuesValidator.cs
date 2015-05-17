using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder.Validation
{
    public class SeparatedValuesValidator : Validator<string[]>
    {
        public SeparatedValuesValidator(char separator)
        {
            this.Separator = separator;
        }

        public char Separator { get; set; }

        protected override ValidatorResult Validate()
        {
            string[] result = Input.Split(Separator);
            return ValidatorResult.CreateValid(result);
        }
        protected override string ToValidForm()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < ValidValue.Length; i++)
            {
                builder.Append(ValidValue[i]);
                if (i != ValidValue.Length - 1) builder.Append(",");
            }
            return builder.ToString();
        }
    }
}
