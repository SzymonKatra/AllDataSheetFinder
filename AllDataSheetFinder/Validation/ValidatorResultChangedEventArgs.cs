using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder.Validation
{
    public class ValidatorResultChangedEventArgs<T> : EventArgs
    {
        public ValidatorResultChangedEventArgs(T oldResult, T newResult)
        {
            this.OldResult = oldResult;
            this.NewResult = newResult;
        }

        public T OldResult { get; set; }
        public T NewResult { get; set; }
    }
}
