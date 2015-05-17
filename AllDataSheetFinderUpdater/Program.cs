using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AllDataSheetFinderUpdater
{
    class Program
    {
        private static readonly string AppMutexName = "AllDataSheetFinder_32366CEF-0521-4213-925D-1EB0299921E7";
        private static readonly string UpdaterMutexName = "AllDataSheetFinderUpdater_0D8C8D15-EDE3-423C-81E9-871FEF848AE0";

        private static Mutex m_oneInstanceMutex;

        static void Main(string[] args)
        {
            Mutex tmpMutex;
            if (Mutex.TryOpenExisting(UpdaterMutexName, out tmpMutex)) return; // checks if updater is already running

            m_oneInstanceMutex = new Mutex(true, UpdaterMutexName);

            while (Mutex.TryOpenExisting(AppMutexName, out tmpMutex))
            {
                tmpMutex.Close();
                Thread.Sleep(1000); // checks if application is closed
            }

            if (args.Length < 2) return;

            string sourcePath = args[args.Length - 2];
            string destinationPath = args[args.Length - 1];

            if (!Directory.Exists(sourcePath)) return;

            if (!Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);
            foreach (string file in Directory.EnumerateFiles(destinationPath, "*.*", SearchOption.AllDirectories))
            {
                File.Delete(file);
            }
            foreach (string file in Directory.EnumerateDirectories(destinationPath, "*", SearchOption.AllDirectories))
            {
                Directory.Delete(file, true);
            }

            foreach (string file in Directory.EnumerateFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                string relativeFilePath = file.Substring(sourcePath.Length + 1);
                string resultFilePath = destinationPath + Path.DirectorySeparatorChar + relativeFilePath;
                if (!Directory.Exists(Path.GetDirectoryName(resultFilePath))) Directory.CreateDirectory(Path.GetDirectoryName(resultFilePath));
                File.Copy(file, resultFilePath);
            }

            Process.Start(destinationPath + Path.DirectorySeparatorChar + "AllDataSheetFinder.exe");
        }
    }
}
