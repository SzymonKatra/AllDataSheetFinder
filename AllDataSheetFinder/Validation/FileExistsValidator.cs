using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder.Validation
{
    public class FileExistsValidator : Validator<string>
    {
        protected override ValidatorResult Validate()
        {
            if (!File.Exists(Input)) return ValidatorResult.CreateInvalid(Global.GetStringResource("StringFileNotExists"));

            return ValidatorResult.CreateValid(Input);
        }
    }
}
