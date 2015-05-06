using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MVVMUtils;
using System.Collections.ObjectModel;

namespace AllDataSheetFinder.Controls
{
    public enum MessageBoxExButton
    {
        Ok,
        Yes,
        No,
    }

    public enum MessageBoxExPredefinedButtons
    {
        Ok,
        YesNo,
    }

    /// <summary>
    /// Interaction logic for MessageBoxEx.xaml
    /// </summary>
    public partial class MessageBoxEx : Window
    {
        public MessageBoxEx(string message, string caption, MessageBoxExPredefinedButtons buttons)
        {
            m_clickCommand = new RelayCommand<MessageBoxExButton>(Click);

            m_message = message;
            m_caption = caption;

            m_buttons = new ObservableCollection<MessageBoxExButton>();
            switch(buttons)
            {
                case MessageBoxExPredefinedButtons.Ok:
                    m_buttons.Add(MessageBoxExButton.Ok);
                    break;
                case MessageBoxExPredefinedButtons.YesNo:
                    m_buttons.Add(MessageBoxExButton.Yes);
                    m_buttons.Add(MessageBoxExButton.No);
                    break;
            }

            this.DataContext = this;

            InitializeComponent();
        }
        public MessageBoxEx(string message, string caption, IEnumerable<MessageBoxExButton> buttons)
        {
            m_clickCommand = new RelayCommand<MessageBoxExButton>(Click);

            m_message = message;
            m_caption = caption;

            m_buttons = new ObservableCollection<MessageBoxExButton>(buttons);

            this.DataContext = this;

            InitializeComponent();
        }

        private ObservableCollection<MessageBoxExButton> m_buttons;
        public ObservableCollection<MessageBoxExButton> Buttons
        {
            get { return m_buttons; }
        }

        private string m_caption;
        public string Caption
        {
            get { return m_caption; }
        }

        private string m_message;
        public string Message
        {
            get { return m_message; }
        }

        private MessageBoxExButton m_result;
        public MessageBoxExButton Result
        {
            get { return m_result; }
            set { m_result = value; }
        }

        private RelayCommand<MessageBoxExButton> m_clickCommand;
        public ICommand ClickCommand
        {
            get { return m_clickCommand; }
        }

        private void Click(MessageBoxExButton button)
        {
            m_result = button;
            this.Close();
        }
    }
}
