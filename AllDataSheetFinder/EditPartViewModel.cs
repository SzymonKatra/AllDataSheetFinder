using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVMUtils;
using System.Windows.Input;
using AllDataSheetFinder.Validation;
using AllDataSheetFinder.Controls;

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
            m_refreshCommand = new RelayCommand(Refresh, CanRefresh);
            m_rebuildTagsCommand = new RelayCommand(RebuildTags);

            m_part = part;

            m_validators = new ValidatorCollection(() => m_okCommand.RaiseCanExecuteChanged());

            m_name = new NoWhitespaceValidator();    
            m_validators.Add(m_name);

            m_tags = new SeparatedValuesValidator(',');
            m_validators.Add(m_tags);

            m_customPath = new FileExistsValidator();
            if (m_part.Custom) m_validators.Add(m_customPath);

            m_isCustom = m_part.Custom;

            ApplyData();
        }

        private PartViewModel m_part;

        private EditPartResult m_result = EditPartResult.Cancel;
        public EditPartResult Result
        {
            get { return m_result; }
        }

        private ValidatorCollection m_validators;

        private NoWhitespaceValidator m_name;
        public NoWhitespaceValidator Name
        {
            get { return m_name; }
        }

        private string m_description;
        public string Description
        {
            get { return m_description; }
            set { m_description = value; RaisePropertyChanged("Description"); }
        }

        private string m_manufacturer;
        public string Manufacturer
        {
            get { return m_manufacturer; }
            set { m_manufacturer = value; RaisePropertyChanged("Manufacturer"); }
        }

        private string m_manufacturerLogo;
        public string ManufacturerLogo
        {
            get { return m_manufacturerLogo; }
            set { m_manufacturerLogo = value; RaisePropertyChanged("ManufacturerLogo"); }
        }

        private string m_manufacturerSite;
        public string ManufacturerSite
        {
            get { return m_manufacturerSite; }
            set { m_manufacturerSite = value; RaisePropertyChanged("ManufacturerSite"); }
        }

        private SeparatedValuesValidator m_tags;
        public SeparatedValuesValidator Tags
        {
            get { return m_tags; }
        }

        private FileExistsValidator m_customPath;
        public FileExistsValidator CustomPath
        {
            get { return m_customPath; }
        }

        private bool m_isCustom;
        public bool IsCustom
        {
            get { return m_isCustom; }
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

        private RelayCommand m_refreshCommand;
        public ICommand RefreshCommand
        {
            get { return m_refreshCommand; }
        }

        private RelayCommand m_rebuildTagsCommand;
        public ICommand RebuildTagsCommand
        {
            get { return m_rebuildTagsCommand; }
        }

        private void Ok(object param)
        {
            m_part.Name = m_name.ValidValue;
            m_part.Description = m_description;
            m_part.Manufacturer = m_manufacturer;
            m_part.ManufacturerImageLink = m_manufacturerLogo;
            m_part.ManufacturerSite = m_manufacturerSite;
            m_part.Tags.Clear();
            foreach (var item in m_tags.ValidValue)
            {
                m_part.Tags.Add(new ValueViewModel<string>(item.RemoveAll(x => char.IsWhiteSpace(x) || x == ',')));
            }

            m_result = EditPartResult.Ok;
            Global.Dialogs.Close(this);
        }

        private void Cancel(object param)
        {
            m_result = EditPartResult.Cancel;
            Global.Dialogs.Close(this);
        }
        
        private async void Refresh(object param)
        {
            if (Global.MessageBox(this, Global.GetStringResource("StringDoYouWantUpdateMoreInfo"), MessageBoxExPredefinedButtons.YesNo) != MessageBoxExButton.Yes) return;
            await m_part.RequestMoreInfo();
            m_part.RebuildTags();
            ApplyData();
        }
        private bool CanRefresh(object param)
        {
            return !m_isCustom;
        }
        
        private void RebuildTags(object param)
        {
            if (Global.MessageBox(this, Global.GetStringResource("StringDoYouWantToRebuildTagsFromDescription"), MessageBoxExPredefinedButtons.YesNo) != MessageBoxExButton.Yes) return;
            m_part.RebuildTags();
            ApplyData();
        }

        private void ApplyData()
        {
            m_name.ValidValue = m_part.Name;
            string[] tokens = new string[m_part.Tags.Count];
            for (int i = 0; i < m_part.Tags.Count; i++) tokens[i] = m_part.Tags[i].Value;
            m_tags.ValidValue = tokens;
            m_description = m_part.Description;
            m_manufacturer = m_part.Manufacturer;
            m_manufacturerLogo = m_part.ManufacturerImageLink;
            m_manufacturerSite = m_part.ManufacturerSite;
        }
    }
}
