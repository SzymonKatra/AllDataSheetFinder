using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace AllDataSheetFinderUpdater
{
    class Program
    {
        private static readonly string AppMutexName = "AllDataSheetFinder_32366CEF-0521-4213-925D-1EB0299921E7";
        private static readonly string UpdaterMutexName = "AllDataSheetFinderUpdater_0D8C8D15-EDE3-423C-81E9-871FEF848AE0";
        private static readonly int AppClosedSteps = 5;
        private static readonly int AppClosedWait = 1000;
        private static readonly int OperationFailureSteps = 5;
        private static readonly int OperationFailureWait = 500;

        private static Mutex m_oneInstanceMutex;

        [STAThread]
        static void Main(string[] args)
        {
            Mutex tmpMutex;
            if (Mutex.TryOpenExisting(UpdaterMutexName, out tmpMutex)) return; // checks if updater is already running

            m_oneInstanceMutex = new Mutex(true, UpdaterMutexName);

            int step = 0;
            while (Mutex.TryOpenExisting(AppMutexName, out tmpMutex))
            {
                tmpMutex.Close();
                Thread.Sleep(1000); // checks if application is closed

                if (++step >= AppClosedSteps)
                {
                    MessageBox.Show("AllDataSheetFinder is still running and updater cant't install new version");
                    return;
                }
            }

            if (args.Length < 2) return;

            string sourcePath = args[args.Length - 2];
            string destinationPath = args[args.Length - 1];

            if (!Directory.Exists(sourcePath)) return;

            if (!Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);
            foreach (string file in Directory.EnumerateFiles(destinationPath, "*.*", SearchOption.AllDirectories))
            {
                int fStep = 0;
                while (true)
                {
                    try
                    {
                        File.Delete(file);
                        break;
                    }
                    catch (Exception e)
                    {
                        if (++fStep >= OperationFailureSteps)
                        {
                            MessageBox.Show($"Problem while deleting file:{Environment.NewLine}{file}{Environment.NewLine}{e.Message}");
                            break;
                        }
                    }

                    Thread.Sleep(OperationFailureWait);
                }
            }
            foreach (string directory in Directory.EnumerateDirectories(destinationPath, "*", SearchOption.AllDirectories))
            {
                int dStep = 0;
                while (true)
                {
                    try
                    {
                        Directory.Delete(directory, true);
                        break;
                    }
                    catch (Exception e)
                    {
                        if (++dStep >= OperationFailureSteps)
                        {
                            MessageBox.Show($"Problem while deleting directory:{Environment.NewLine}{directory}{Environment.NewLine}{e.Message}");
                            break;
                        }
                    }

                    Thread.Sleep(OperationFailureWait);
                }
            }

            foreach (string file in Directory.EnumerateFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                string relativeFilePath = file.Substring(sourcePath.Length + 1);
                string resultFilePath = $"{destinationPath}{Path.DirectorySeparatorChar}{relativeFilePath}";

                int cStep = 0;
                while (true)
                {
                    try
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(resultFilePath))) Directory.CreateDirectory(Path.GetDirectoryName(resultFilePath));
                        File.Copy(file, resultFilePath);
                        break;
                    }
                    catch (Exception e)
                    {
                        if (++cStep >= OperationFailureSteps)
                        {
                            MessageBox.Show($"Problem while copying file from:{Environment.NewLine}{file} to: {Environment.NewLine}{resultFilePath}{Environment.NewLine }{e.Message}");
                            break;
                        }
                    }

                    Thread.Sleep(OperationFailureWait);
                }
            }

            Process.Start($"{destinationPath}{Path.DirectorySeparatorChar}AllDataSheetFinder.exe");

            m_oneInstanceMutex.Close();
        }
    }
}
