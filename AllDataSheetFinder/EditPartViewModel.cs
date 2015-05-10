using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVMUtils;
using System.Windows.Input;

namespace AllDataSheetFinder
{
    public class EditPartViewModel : ObservableObject
    {
        public enum EditPartResult
        {
            Ok,
            Cancel
        }

        public EditPartViewModel(PartViewModel part)
        {
            m_part = part;
        }

        private PartViewModel m_part;

        private EditPartResult m_result = EditPartResult.Cancel;
        public EditPartResult Result
        {
            get { return m_result; }
        }

        private RelayCommand m_okCommand;
        public ICommand OkCommand
        {
            get { return m_okCommand; }
        }

        private RelayCommand m_cancelCommand;
        public ICommand CancelCommand
        {
            get { return m_cancelCommand; }
        }

        private void Ok(object param)
        {
            m_result = EditPartResult.Ok;
            Global.Dialogs.Close(this);
        }
        private void Cancel(object param)
        {
            m_result = EditPartResult.Cancel;
            Global.Dialogs.Close(this);
        }
    }
}
