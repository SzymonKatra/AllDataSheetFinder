using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder.Validation
{
    public interface IValidator
    {
        bool IsValid { get; }
        string Error { get; }
        event EventHandler IsValidChanged;

        void ForceValidate();
    }
}
