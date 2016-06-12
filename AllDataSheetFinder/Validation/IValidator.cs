using System;

namespace AllDataSheetFinder.Validation
{
    public interface IValidator
    {
        bool IsValid { get; }
        string Error { get; }
        event EventHandler IsValidChanged;
    }
}
