using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Media.Imaging;
using MVVMUtils;
using AllDataSheetFinder.Controls;
using System.Globalization;
using Microsoft.Win32;

namespace AllDataSheetFinder
{
    public static class Global
    {
        static Global()
        {
            s_dialogs = new DialogService();
            s_dialogs.AddMapping(typeof(SettingsViewModel), typeof(SettingsWindow));
            s_dialogs.AddMapping(typeof(EditPartViewModel), typeof(EditPartWindow));
            s_dialogs.AddMapping(typeof(ActionDialogViewModel), typeof(ActionDialogWindow));
            s_dialogs.AddMapping(typeof(UpdateViewModel), typeof(UpdateWindow));
        }

        public static readonly string RegistryKeyName = @"Software\AllDataSheetFinder";

        public static string AppDataPath { get; private set; }
        public static readonly string ImagesCacheDirectory = "Cache" + Path.DirectorySeparatorChar + "Images";
        public static readonly string DatasheetsCacheDirectory = "Cache" + Path.DirectorySeparatorChar + "Datasheets";
        public static readonly string SavedDatasheetsDirectory = "SavedDatasheets";
        public static readonly string UpdateFile = "update.zip";
        public static readonly string UpdateExtractDirectory = "Update";
        public static readonly string ConfigFile = "config.xml";
        public static readonly string SavedPartsFile = SavedDatasheetsDirectory + Path.DirectorySeparatorChar + "parts.xml";
        public static readonly string LanguagesDirectory = AppDomain.CurrentDomain.BaseDirectory + "Languages";
        public static readonly string ErrorLogFileName = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar + "AllDatasheetFinder.log";

        public static readonly string MutexName = "AllDataSheetFinder_32366CEF-0521-4213-925D-1EB0299921E7";

        public static readonly string UpdateVersionLink = "https://www.dropbox.com/s/ypmujj9ikjl8nf1/update_info.xml?dl=1";

        private static XmlSerializer s_serializerConfig = new XmlSerializer(typeof(Config));
        private static XmlSerializer s_serialzierSavedParts = new XmlSerializer(typeof(List<Part>));

        private static ResourceDictionary s_stringsDictionary;

        private static Config s_configuration;
        public static Config Configuration
        {
            get { return s_configuration; }
        }

        private static DialogService s_dialogs;
        public static DialogService Dialogs
        {
            get { return s_dialogs; }
            set { s_dialogs = value; }
        }

        private static MainViewModel s_main;
        public static MainViewModel Main
        {
            get { return s_main; }
            set
            {
                if (s_main != null) throw new InvalidOperationException("Main already set");
                s_main = value;
            }
        }

        private static Dictionary<string, BitmapImageLoadingInfo> s_cachedImages = new Dictionary<string, BitmapImageLoadingInfo>();
        public static Dictionary<string, BitmapImageLoadingInfo> CachedImages
        {
            get { return s_cachedImages; }
        }

        private static List<Part> s_savedParts = new List<Part>();
        public static List<Part> SavedParts
        {
            get { return s_savedParts; }
        }

        public static string GetStringResource(object key)
        {
            object result = Application.Current.TryFindResource(key);
            return (result == null ? key + " NOT FOUND - RESOURCE ERROR" : (string)result);
        }
        public static MessageBoxExButton MessageBox(object viewModel, string text, MessageBoxExPredefinedButtons buttons)
        {
            //return MessageBoxSuper.ShowBox(Dialogs.GetWindow(viewModel), text, GetStringResource("StringAppName"), buttons);
            MessageBoxEx mbox = new MessageBoxEx(text, GetStringResource("StringAppName"), buttons);
            mbox.Owner = Dialogs.GetWindow(viewModel);
            mbox.ShowDialog();
            return mbox.Result;
        }

        public static void ApplyLanguage()
        {
            ApplyLanguage(Configuration.Language);
        }
        public static void ApplyLanguage(string language)
        {
            if (language == "en-US")
            {
                s_stringsDictionary.Source = new Uri(@"Resources\Strings.xaml", UriKind.Relative);
                return;
            }

            if (string.IsNullOrEmpty(language))
            {
                string path = LanguagesDirectory + Path.DirectorySeparatorChar + "Strings." + CultureInfo.CurrentCulture.Name + ".xaml";
                if (File.Exists(path))
                {
                    s_stringsDictionary.Source = new Uri(path, UriKind.Absolute);
                }
                else
                {
                    s_stringsDictionary.Source = new Uri(@"Resources\Strings.xaml", UriKind.Relative);
                }
                return;
            }

            foreach (string langPath in Directory.EnumerateFiles(LanguagesDirectory))
            {
                string file = Path.GetFileName(langPath);
                string[] tokens = file.Split('.');
                if (tokens.Length < 2) continue;
                if (tokens[1].Contains(language))
                {
                    s_stringsDictionary.BeginInit();
                    s_stringsDictionary.Source = new Uri(langPath, UriKind.Absolute);
                    s_stringsDictionary.EndInit();
                    return;
                }
            }

            s_stringsDictionary.Source = new Uri(@"Resources\Strings.xaml", UriKind.Relative);
        }

        public static void InitializeAll()
        {
            AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "AllDataSheetFinder";

            RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyName);
            object dataPathObj = key.GetValue("DataPath", null);
            if (dataPathObj != null) AppDataPath = dataPathObj.ToString(); else key.SetValue("DataPath", AppDataPath);
            key.Close();

            CreateDirectoriesIfNeeded();
            LoadConfiguration();

            s_stringsDictionary = Application.Current.Resources.MergedDictionaries.Where(x => x.Source.ToString().Contains(@"Resources\Strings.xaml")).ElementAt(0);

            ApplyLanguage();

            string datasheetCachePath = AppDataPath + Path.DirectorySeparatorChar + DatasheetsCacheDirectory;
            DirectoryInfo dir = new DirectoryInfo(datasheetCachePath);
            IEnumerable<FileInfo> cachedDatasheets = dir.EnumerateFiles();

            long size = 0;
            foreach (var item in cachedDatasheets)
            {
                size += item.Length;
            }

            if (size > Configuration.MaxDatasheetsCacheSize)
            {
                List<FileInfo> files = cachedDatasheets.ToList();
                files.Sort((x, y) => x.LastAccessTime.CompareTo(y.LastAccessTime));
                for (int i = 0; i < files.Count; i++)
                {
                    size -= files[i].Length;
                    files[i].Delete();

                    if (size < Configuration.MaxDatasheetsCacheSize) break;
                }
            }

            LoadSavedParts();
        }
        public static void ApplyAppDataPath(string newPath)
        {
            if (Directory.Exists(newPath)) Directory.Delete(newPath);
            Directory.Move(AppDataPath, newPath);

            RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyName);
            key.SetValue("DataPath", newPath);
            key.Close();

            AppDataPath = newPath;
        }

        public static void CreateDirectoriesIfNeeded()
        {
            string path = AppDataPath + Path.DirectorySeparatorChar + ImagesCacheDirectory;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = AppDataPath + Path.DirectorySeparatorChar + DatasheetsCacheDirectory;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = AppDataPath + Path.DirectorySeparatorChar + SavedDatasheetsDirectory;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        public static void LoadConfiguration()
        {
            string path = AppDataPath + Path.DirectorySeparatorChar + ConfigFile;
            if (!File.Exists(path))
            {
                s_configuration = new Config();
                s_configuration.MaxDatasheetsCacheSize = 100 * 1024 * 1024; // 100 MiB
                s_configuration.Language = string.Empty;
                SaveConfiguration();
            }
            else
            {
                using (FileStream file = new FileStream(path, FileMode.Open))
                {
                    try
                    {
                        s_configuration = (Config)s_serializerConfig.Deserialize(file);
                    }
                    catch
                    {
                        file.Close();
                        File.Delete(path);
                        LoadConfiguration();
                    }
                }
            }
        }
        public static void SaveConfiguration()
        {
            string path = AppDataPath + Path.DirectorySeparatorChar + ConfigFile;
            using (FileStream file = new FileStream(path, FileMode.Create)) s_serializerConfig.Serialize(file, s_configuration);
        }

        public static void LoadSavedParts()
        {
            string path = AppDataPath + Path.DirectorySeparatorChar + SavedPartsFile;
            if (File.Exists(path))
            {
                using (FileStream file = new FileStream(path, FileMode.Open))
                {
                    try
                    {
                        s_savedParts = (List<Part>)s_serialzierSavedParts.Deserialize(file);
                    }
                    catch
                    {
                        file.Close();
                        File.Delete(path);
                    }
                }
            }
        }
        public static void SaveSavedParts()
        {
            CreateDirectoriesIfNeeded();
            string path = AppDataPath + Path.DirectorySeparatorChar + SavedPartsFile;
            using (FileStream file = new FileStream(path, FileMode.Create)) s_serialzierSavedParts.Serialize(file, s_savedParts);
        }

        public static string BuildSavedDatasheetPath(string code)
        {
            return AppDataPath + Path.DirectorySeparatorChar + SavedDatasheetsDirectory + Path.DirectorySeparatorChar + code + ".pdf";
        }
        public static string BuildCachedDatasheetPath(string code)
        {
            return AppDataPath + Path.DirectorySeparatorChar + DatasheetsCacheDirectory + Path.DirectorySeparatorChar + code + ".pdf";
        }
    }
}
