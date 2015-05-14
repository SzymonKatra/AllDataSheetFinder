using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder.Validation
{
    public class NoWhitespaceValidator : Validator<string>
    {
        protected override ValidatorResult<string> ProcessValidation()
        {
            foreach (char c in Input)
            {
                if (char.IsWhiteSpace(c)) return ValidatorResult<string>.CreateInvalid(Global.GetStringResource("StringIllegalCharacters"));
            }

            return ValidatorResult<string>.CreateValid(Input);
        }
    }
}
