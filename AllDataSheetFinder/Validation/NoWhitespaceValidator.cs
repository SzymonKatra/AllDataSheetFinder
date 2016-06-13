namespace AllDataSheetFinder.Validation
{
    public class NoWhitespaceValidator : Validator<string>
    {
        protected override ValidatorResult Validate()
        {
            foreach (char c in Input)
            {
                if (char.IsWhiteSpace(c)) return ValidatorResult.CreateInvalid(Global.GetStringResource("StringIllegalCharacters"));
            }

            return ValidatorResult.CreateValid(Input);
        }
    }
}
