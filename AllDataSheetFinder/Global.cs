﻿using System;
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

namespace AllDataSheetFinder
{
    public static class Global
    {
        static Global()
        {
            s_dialogs = new DialogService();
            s_dialogs.AddMapping(typeof(SettingsViewModel), typeof(SettingsWindow));
        }

        public static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "AllDataSheetFinder";
        public static readonly string ImagesCacheDirectory = "Cache" + Path.DirectorySeparatorChar + "Images";
        public static readonly string DatasheetsCacheDirectory = "Cache" + Path.DirectorySeparatorChar + "Datasheets";
        public static readonly string SavedDatasheetsDirectory = "SavedDatasheets";
        public static readonly string ConfigFile = "config.xml";
        public static readonly string SavedPartsFile = SavedDatasheetsDirectory + Path.DirectorySeparatorChar + "parts.xml";

        private static XmlSerializer s_serializerConfig = new XmlSerializer(typeof(Config));
        private static XmlSerializer s_serialzierSavedParts = new XmlSerializer(typeof(List<SavedPart>));

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

        private static List<SavedPart> s_savedParts = new List<SavedPart>();
        public static List<SavedPart> SavedParts
        {
            get { return s_savedParts; }
        }

        private static Dictionary<string, PartDatasheetState> s_downloadList = new Dictionary<string, PartDatasheetState>();
        public static Dictionary<string, PartDatasheetState> DownloadList
        {
            get { return s_downloadList; }
        }

        private static object s_downloadListLock = new object();
        public static object DownloadListLock
        {
            get { return s_downloadListLock; }
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

        public static void InitializeAll()
        {
            CreateDirectoriesIfNeeded();
            LoadConfiguration();

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

            Dictionary<SavedPart, string> codes = new Dictionary<SavedPart,string>();
            foreach (var item in SavedParts)
            {
                codes.Add(item, AllDataSheetPart.BuildCodeFromLink(item.DatasheetSiteLink, item.Name, item.Manufacturer, item.DatasheetSiteLink.GetHashCode().ToString()));
            }

            List<SavedPart> toRemove = new List<SavedPart>();
            foreach (var item in codes)
            {
                if (!File.Exists(BuildSavedDatasheetPath(item.Value))) toRemove.Add(item.Key);
            }
            foreach (var item in toRemove) SavedParts.Remove(item);

            foreach (string file in Directory.EnumerateFiles(AppDataPath + Path.DirectorySeparatorChar + SavedDatasheetsDirectory))
            {
                if (Path.GetExtension(file) != ".pdf") continue;
                string code = Path.GetFileNameWithoutExtension(file);
                if (!codes.ContainsValue(code)) File.Delete(file);
            }
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
                SaveConfiguration();
            }
            else
            {
                using (FileStream file = new FileStream(path, FileMode.Open)) s_configuration = (Config)s_serializerConfig.Deserialize(file);
            }
        }
        public static void SaveConfiguration()
        {
            string path = AppDataPath + Path.DirectorySeparatorChar + ConfigFile;
            using (FileStream file = new FileStream(path, FileMode.OpenOrCreate)) s_serializerConfig.Serialize(file, s_configuration);
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
                        s_savedParts = (List<SavedPart>)s_serialzierSavedParts.Deserialize(file);
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
            string path = AppDataPath + Path.DirectorySeparatorChar + SavedPartsFile;
            using (FileStream file = new FileStream(path, FileMode.OpenOrCreate)) s_serialzierSavedParts.Serialize(file, s_savedParts);
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
