using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVMUtils;

namespace AllDataSheetFinder
{
    public class ValueViewModel<T> : DataViewModelBase<ValueViewModel<T>, T>
    {
        public ValueViewModel()
            : this(default(T))
        {
        }
        public ValueViewModel(T value)
            : base(value)
        {
        }

        public T Value
        {
            get { return base.Model; }
            set { base.Model = value; RaisePropertyChanged(nameof(Value)); }
        }

        protected override void OnPopCopy(WorkingCopyResult result)
        {
            ObjectsPack pack = CopyStack.Pop();
            this.Value = (T)pack.Read();
        }
        protected override void OnPushCopy()
        {
            ObjectsPack pack = new ObjectsPack();
            pack.Write(this.Value);
            CopyStack.Push(pack);
        }
    }
}
