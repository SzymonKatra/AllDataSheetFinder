using System.IO;

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
