using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace AllDataSheetFinder.Validation
{
    public class ValidatorCollection : Collection<IValidator>
    {
        public ValidatorCollection(Action isValidChangedAction)
        {
            m_isValidChangedAction = isValidChangedAction;
        }

        private Action m_isValidChangedAction;

        public bool IsValidAll
        {
            get
            {
                return this.Count == this.Count(x => x.IsValid);
            }
        }      

        protected override void ClearItems()
        {
            foreach (var item in this)
            {
                item.IsValidChanged -= item_IsValidChanged;
            }
            base.ClearItems();
        }
        protected override void InsertItem(int index, IValidator item)
        {
            item.IsValidChanged += item_IsValidChanged;
            base.InsertItem(index, item);
        }
        protected override void RemoveItem(int index)
        {
            this[index].IsValidChanged -= item_IsValidChanged;
            base.RemoveItem(index);
        }
        protected override void SetItem(int index, IValidator item)
        {
            this[index].IsValidChanged -= item_IsValidChanged;
            item.IsValidChanged += item_IsValidChanged;
            base.SetItem(index, item);
        }

        private void item_IsValidChanged(object sender, EventArgs e)
        {
            m_isValidChangedAction?.Invoke();
        }
    }
}
