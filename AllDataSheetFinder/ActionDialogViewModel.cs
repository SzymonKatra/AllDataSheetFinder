using MVVMUtils;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AllDataSheetFinder
{
    public class ActionDialogViewModel : ObservableObject
    {
        public ActionDialogViewModel(Task task)
            : this(task, string.Empty)
        {
        }
        public ActionDialogViewModel(Task task, string message)
        {
            m_runWorkCommand = new RelayCommand(RunWork);

            m_task = task;
            m_message = message;
        }

        private Task m_task;

        private string m_message;
        public string Message
        {
            get { return m_message; }
            set { m_message = value; RaisePropertyChanged(nameof(Message)); }
        }

        private RelayCommand m_runWorkCommand;
        public ICommand RunWorkCommand
        {
            get { return m_runWorkCommand; }
        }

        private async void RunWork(object param)
        {
            await m_task;
            Global.Dialogs.Close(this);
        }
    }
}
