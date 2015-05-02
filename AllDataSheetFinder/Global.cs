using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MVVMUtils;

namespace AllDataSheetFinder
{
    public static class Global
    {
        private static DialogService m_dialogs;
        public static DialogService Dialogs
        {
            get { return m_dialogs; }
            set { m_dialogs = value; }
        }

        private static MainViewModel m_main;
        public static MainViewModel Main
        {
            get { return m_main; }
            set
            {
                if (m_main != null) throw new InvalidOperationException("Main already set");
                m_main = value;
            }
        }

        static Global()
        {
            m_dialogs = new DialogService();
        }

        public static string GetStringResource(object key)
        {
            object result = Application.Current.TryFindResource(key);
            return (result == null ? key + " NOT FOUND - RESOURCE ERROR" : (string)result);
        }
        public static MessageBoxSuperButton MessageBox(object viewModel, string text, MessageBoxSuperPredefinedButtons buttons)
        {
            return MessageBoxSuper.ShowBox(Dialogs.GetWindow(viewModel), text, GetStringResource("StringAppName"), buttons);
        }
    }
}
