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

        public Global()
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
