using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AllDataSheetFinder
{
    public static class DirectoryExt
    {
        public static void Copy(string sourceDirName, string destinationDirName)
        {
            if (!Directory.Exists(destinationDirName)) Directory.CreateDirectory(destinationDirName);
            foreach (var item in Directory.EnumerateFiles(sourceDirName, "*.*", SearchOption.AllDirectories))
            {
                string relative = item.Substring(sourceDirName.Length + 1);
                string result = Path.Combine(destinationDirName, relative);
                string dir = Path.GetDirectoryName(result);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.Copy(item, result);
            }
        }
    }
}
