using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVMUtils;
using System.Windows.Input;
using AllDataSheetFinder.Validation;

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
            m_okCommand = new RelayCommand(Ok);
            m_cancelCommand = new RelayCommand(Cancel);

            m_part = part;

            m_validators = new ValidatorCollection(() => m_okCommand.RaiseCanExecuteChanged());
        }

        private PartViewModel m_part;

        private EditPartResult m_result = EditPartResult.Cancel;
        public EditPartResult Result
        {
            get { return m_result; }
        }

        private ValidatorCollection m_validators;

        private string m_name;
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        private string m_description;
        public string Description
        {
            get { return m_description; }
            set { m_description = value; }
        }

        private string m_manufacturer;
        public string Manufacturer
        {
            get { return m_manufacturer; }
            set { m_manufacturer = value; }
        }

        private string m_manufacturerLogo;
        public string ManufacturerLogo
        {
            get { return m_manufacturerLogo; }
            set { m_manufacturerLogo = value; }
        }

        private string m_maufacturerSite;
        public string MaufacturerSite
        {
            get { return m_maufacturerSite; }
            set { m_maufacturerSite = value; }
        }

        private string m_size;
        public string Size
        {
            get { return m_size; }
            set { m_size = value; }
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
