﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVMUtils;
using System.IO;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Diagnostics;

namespace AllDataSheetFinder
{
    public class PartViewModel : ObservableObject, IModelExposable<Part>, IWorkingCopyAvailable
    {
        public PartViewModel()
            : this(new Part(), false)
        {
        }
        public PartViewModel(Part model, bool modelValid = true)
        {
            m_model = model;
            m_moreInfoState = PartMoreInfoState.Available;
            if (modelValid)
            {
                MakeContext();
                CheckState();
            }
        }

        public static PartViewModel FromAllDataSheetPart(AllDataSheetPart part)
        {
            PartViewModel result = new PartViewModel();
            result.Name = part.Name;
            result.Description = part.Description;
            result.Manufacturer = part.Manufacturer;
            result.ManufacturerImageLink = part.ManufacturerImageLink;
            result.DatasheetSiteLink = part.DatasheetSiteLink;
            result.Context = part;
            result.m_moreInfoState = PartMoreInfoState.NotAvailable;
            result.CheckState();
            return result;
        }

        private static object s_downloadListLock = new object();
        private static Dictionary<string, PartDatasheetState> s_downloadList = new Dictionary<string, PartDatasheetState>();

        private Part m_model;
        public Part Model
        {
            get { return m_model; }
        }

        public string Name
        {
            get { return m_model.Name; }
            set { m_model.Name = value; RaisePropertyChanged("Name"); RaisePropertyChanged("Code"); }
        }
        public string Description
        {
            get { return m_model.Description; }
            set { m_model.Description = value; RaisePropertyChanged("Description"); }
        }
        public string Manufacturer
        {
            get { return m_model.Manufacturer; }
            set { m_model.Manufacturer = value; RaisePropertyChanged("Manufacturer"); RaisePropertyChanged("Code"); }
        }
        public string ManufacturerImageLink
        {
            get { return m_model.ManufacturerImageLink; }
            set { m_model.ManufacturerImageLink = value; RaisePropertyChanged("ManufacturerImageLink"); RaisePropertyChanged("ImageFileName"); }
        }
        public string DatasheetSiteLink
        {
            get { return m_model.DatasheetSiteLink; }
            set { m_model.DatasheetSiteLink = value; RaisePropertyChanged("DatasheetSiteLink"); RaisePropertyChanged("Code"); }
        }
        public string DatasheetPdfLink
        {
            get { return m_model.DatasheetPdfLink; }
            set { m_model.DatasheetPdfLink = value; RaisePropertyChanged("DatasheetPdfLink"); }
        }
        public long DatasheetSize
        {
            get { return m_model.DatasheetSize; }
            set { m_model.DatasheetSize = value; RaisePropertyChanged("DatasheetSize"); RaisePropertyChanged("MoreInfoDisplay"); }
        }
        public int DatasheetPages
        {
            get { return m_model.DatasheetPages; }
            set { m_model.DatasheetPages = value; RaisePropertyChanged("DatasheetPages"); RaisePropertyChanged("MoreInfoDisplay"); }
        }
        public string ManufacturerSite
        {
            get { return m_model.ManufacturerSite; }
            set { m_model.ManufacturerSite = value; RaisePropertyChanged("ManufacturerSite"); RaisePropertyChanged("MoreInfoDisplay"); }
        }
        public DateTime LastUseDate
        {
            get { return m_model.LastUseDate; }
            set { m_model.LastUseDate = value; RaisePropertyChanged("LastUseDate"); }
        }
        public bool Custom
        {
            get { return m_model.Custom; }
            set { m_model.Custom = value; RaisePropertyChanged("Custom"); }
        }
        public string CustomPath
        {
            get { return m_model.CustomPath; }
            set { m_model.CustomPath = value; RaisePropertyChanged("CustomPath"); }
        }

        private PartMoreInfoState m_moreInfoState = PartMoreInfoState.NotAvailable;
        public PartMoreInfoState MoreInfoState
        {
            get { return m_moreInfoState; }
            set { m_moreInfoState = value; RaisePropertyChanged("MoreInfoState"); }
        }
        public string MoreInfoDisplay
        {
            get
            {
                return string.Format(@"{1}: {2} KB{0}{3}: {4}{0}{5}: {6}",
                                     Environment.NewLine,
                                     Global.GetStringResource("StringSize"), DatasheetSize / 1024,
                                     Global.GetStringResource("StringPages"), DatasheetPages,
                                     Global.GetStringResource("StringManufacturerSite"), ManufacturerSite);
            }
        }

        public string Code
        {
            get
            {
                return BuildCodeFromLink(DatasheetSiteLink, Name, Manufacturer, DatasheetSiteLink.GetHashCode().ToString());
            }
        }
        public string ImageFileName
        {
            get
            {
                Uri imageUri = new Uri(ManufacturerImageLink);
                return imageUri.Segments[imageUri.Segments.Length - 1];
            }
        }

        private PartDatasheetState m_state;
        public PartDatasheetState State
        {
            get { return m_state; }
            set { m_state = value; RaisePropertyChanged("State"); }
        }

        private BitmapImage m_image;
        public BitmapImage Image
        {
            get { return m_image; }
            set { m_image = value; RaisePropertyChanged("Image"); }
        }

        private AllDataSheetPart m_context;
        protected AllDataSheetPart Context
        {
            get { return m_context; }
            set { m_context = value; }
        }

        public bool IsContextValid
        {
            get { return m_context != null; }
        }

        public async void LoadImage()
        {
            string file = ImageFileName;
            string imagePath = Global.AppDataPath + Path.DirectorySeparatorChar + Global.ImagesCacheDirectory + Path.DirectorySeparatorChar + file;

            if (!Global.CachedImages.ContainsKey(file)) Global.CachedImages.Add(file, BitmapImageLoadingInfo.CreateDefault());
            BitmapImageLoadingInfo info = Global.CachedImages[file];

            if (info.Loaded) this.Image = info.Image;
            if (info.Loading)
            {
                await Task.Run(() =>
                {
                    // ugly hack to test wheter image is loaded,
                    // assigment of Image.Source before BitmapImage has been loaded causes NullReferenceException in BitmapImage.EndInit()
                    while (info.Loading) Task.Delay(100);
                });
                this.Image = info.Image;
            }
            if (info.Loaded || info.Loading) return;
            info.Loading = true;

            if (File.Exists(imagePath))
            {
                FileStream stream = new FileStream(imagePath, FileMode.Open);
                info.Image.BeginInit();
                info.Image.StreamSource = stream;
                info.Image.EndInit();
            }
            else
            {
                MemoryStream memory = new MemoryStream();
                await Task.Run(() =>
                {
                    Stream stream = m_context.GetManufacturerImageStream();

                    FileStream fileStream = new FileStream(imagePath, FileMode.Create);
                    try
                    {
                        byte[] buffer = new byte[1024];
                        int len;
                        while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            memory.Write(buffer, 0, len);
                            fileStream.Write(buffer, 0, len);
                        }
                    }
                    finally
                    {
                        stream.Close();
                        fileStream.Close();
                    }
                });

                memory.Seek(0, SeekOrigin.Begin);
                info.Image.BeginInit();
                info.Image.StreamSource = memory;
                info.Image.EndInit();
            }

            this.Image = info.Image;

            info.Loaded = true;
            info.Loading = false;
        }
        public async Task RequestMoreInfo()
        {
            if (MoreInfoState == PartMoreInfoState.Downloading)
            {
                await Task.Run(() =>
                {
                    while (MoreInfoState == PartMoreInfoState.Downloading) Task.Delay(100);
                });
                return;
            }

            MoreInfoState = PartMoreInfoState.Downloading;

            AllDataSheetPart.MoreInfo moreInfo = await m_context.RequestMoreInfoAsync();
            DatasheetPdfLink = moreInfo.PdfSite;

            long multiplier = 1;
            if (moreInfo.Size.Contains('K')) multiplier = 1024;
            else if (moreInfo.Size.Contains('M')) multiplier = 1024 * 1024;
            moreInfo.Size = moreInfo.Size.RemoveAll(x => !char.IsDigit(x) && x != '.');
            decimal bytes = decimal.Parse(moreInfo.Size, CultureInfo.InvariantCulture);
            DatasheetSize = (long)decimal.Round(bytes) * multiplier;

            moreInfo.Pages = moreInfo.Pages.RemoveAll(x => !char.IsDigit(x));
            DatasheetPages = int.Parse(moreInfo.Pages);

            ManufacturerSite = moreInfo.ManufacturerSite.RemoveAll(x => char.IsWhiteSpace(x));

            MoreInfoState = PartMoreInfoState.Available;
        }
        public async Task OpenPdf()
        {
            if (State == PartDatasheetState.DownloadingAndOpening) return;
            if (State == PartDatasheetState.Downloading)
            {
                State = PartDatasheetState.DownloadingAndOpening;
                await Task.Run(() =>
                {
                    while (State != PartDatasheetState.Saved) Task.Delay(100);
                });
            }

            string code = Code;
            if (State == PartDatasheetState.Saved)
            {
                string pdfPath = Global.BuildSavedDatasheetPath(code);
                LastUseDate = DateTime.Now;
                Process.Start(pdfPath);
            }
            else if (State == PartDatasheetState.Cached)
            {
                string pdfPath = Global.BuildCachedDatasheetPath(code);
                Process.Start(pdfPath);
            }
            else
            {
                State = PartDatasheetState.DownloadingAndOpening;
                string pdfPath = Global.BuildCachedDatasheetPath(code);
                await DownloadPdf(pdfPath);
                Process.Start(pdfPath);
                CheckState();
            }
        }
        public async Task SavePdf()
        {
            if (State == PartDatasheetState.Downloading) return;
            if (State == PartDatasheetState.DownloadingAndOpening)
            {
                await Task.Run(() =>
                {
                    while (State != PartDatasheetState.Cached) Task.Delay(100);
                });
            }

            string code = Code;

            if (State == PartDatasheetState.NotDownloaded)
            {
                State = PartDatasheetState.Downloading;
                string pdfPath = Global.BuildSavedDatasheetPath(code);
                await DownloadPdf(pdfPath);
                CheckState();
            }

            if (State == PartDatasheetState.Cached)
            {
                File.Move(Global.BuildCachedDatasheetPath(code), Global.BuildSavedDatasheetPath(code));
            }
            CheckState();

            Debug.Assert(State == PartDatasheetState.Saved, "Pdf is not in saved state after downloading!");

            LastUseDate = DateTime.Now;
        }
        public void RemovePdf()
        {
            if (State != PartDatasheetState.Saved) throw new InvalidOperationException("Pdf is not in saved state");

            File.Delete(Global.BuildSavedDatasheetPath(Code));

            CheckState();
        }

        private async Task DownloadPdf(string pdfPath)
        {
            Stream stream = null;
            lock (s_downloadListLock) s_downloadList.Add(Code, State);
            try
            {
                if (m_moreInfoState != PartMoreInfoState.Available) await RequestMoreInfo();
                stream = await m_context.GetDatasheetStreamAsync(DatasheetPdfLink);
                await Task.Run(() =>
                {
                    using (FileStream file = new FileStream(pdfPath, FileMode.Create))
                    {
                        byte[] buffer = new byte[4096];
                        int len;
                        while ((len = stream.Read(buffer, 0, buffer.Length)) > 0) file.Write(buffer, 0, len);
                    }
                });
            }
            finally
            {
                if (stream != null) stream.Close();
                lock (s_downloadListLock) s_downloadList.Remove(Code);
            }
        }

        private void CheckState()
        {
            string code = Code;

            bool isDownloading;
            lock (s_downloadListLock) isDownloading = s_downloadList.ContainsKey(code);
            if (isDownloading)
            {
                lock (s_downloadListLock) State = s_downloadList[code];
                CheckGlobalState();
                return;
            }

            string pdfPath = Global.BuildSavedDatasheetPath(code);
            if (File.Exists(pdfPath))
            {
                State = PartDatasheetState.Saved;
                return;
            }

            pdfPath = Global.BuildCachedDatasheetPath(code);
            if (File.Exists(pdfPath))
            {
                State = PartDatasheetState.Cached;
                return;
            }

            State = PartDatasheetState.NotDownloaded;
        }
        private async void CheckGlobalState()
        {
            string code = Code;
            await Task.Run(() =>
            {
                bool contains;
                lock (s_downloadListLock) contains = s_downloadList.ContainsKey(code);
                while (contains)
                {
                    Task.Delay(100);
                    lock (s_downloadListLock) contains = s_downloadList.ContainsKey(code);
                }
            });
            CheckState();
        }
        public void MakeContext()
        {
            m_context = new AllDataSheetPart(DatasheetSiteLink);
            RaisePropertyChanged("IsContextValid");
        }

        public static string BuildCodeFromLink(string link, string name, string manufacturer, string hash)
        {
            Uri uri = new Uri(link);
            string[] segments = uri.Segments;
            if (segments.Length < 3) return BuildCode(name, manufacturer, link.GetHashCode().ToString());
            string nameSegment = segments[segments.Length - 1];
            string manufacturerSegment = segments[segments.Length - 2];
            string numberSegment = segments[segments.Length - 3];

            nameSegment = nameSegment.Remove(nameSegment.Length - 5); // remove ".html"
            manufacturerSegment = manufacturerSegment.Remove(manufacturerSegment.Length - 1); // remove slash
            numberSegment = numberSegment.Remove(numberSegment.Length - 1); // remove slash

            return BuildCode(nameSegment, manufacturerSegment, numberSegment);
        }
        public static string BuildCode(string name, string manufacturer, string hash)
        {
            name = ToValidCodeForm(name);
            manufacturer = ToValidCodeForm(manufacturer);
            hash = ToValidCodeForm(hash);

            return string.Format("{0}_{1}_{2}", name, manufacturer, hash);
        }

        private static string ToValidCodeForm(string value)
        {
            return value.Replace(' ', '-').RemoveAll(x => !char.IsLetterOrDigit(x));
        }

        public int CopyDepth
        {
            get { throw new NotImplementedException(); }
        }
        public void PopCopy(WorkingCopyResult result)
        {
            throw new NotImplementedException();
        }
        public void PushCopy()
        {
            throw new NotImplementedException();
        }
    }
}
