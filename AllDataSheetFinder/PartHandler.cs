using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using MVVMUtils;
using System.Globalization;

namespace AllDataSheetFinder
{
    public class PartHandler : ObservableObject
    {
        private AllDataSheetPart m_part;
        public AllDataSheetPart Part
        {
            get { return m_part; }
            set { m_part = value; RaisePropertyChanged("Part"); }
        }

        private BitmapImage m_image;
        public BitmapImage Image
        {
            get { return m_image; }
            set { m_image = value; RaisePropertyChanged("Image"); }
        }

        private PartDatasheetState m_state;
        public PartDatasheetState State
        {
            get { return m_state; }
            set { m_state = value; RaisePropertyChanged("State"); }
        }

        public string ImageFileName
        {
            get
            {
                Uri imageUri = new Uri(m_part.ManufacturerImageLink);
                return imageUri.Segments[imageUri.Segments.Length - 1];
            }
        }

        private PartMoreInfoState m_moreInfoState = PartMoreInfoState.NotAvailable;
        public PartMoreInfoState MoreInfoState
        {
            get { return m_moreInfoState; }
            set { m_moreInfoState = value; RaisePropertyChanged("MoreInfoState"); }
        }

        private string m_datasheetPdfSite;
        public string DatasheetPdfSite
        {
            get { return m_datasheetPdfSite; }
            set { m_datasheetPdfSite = value; RaisePropertyChanged("DatasheetPdfSite"); }
        }

        private long m_datasheetSize;
        public long DatasheetSize
        {
            get { return m_datasheetSize; }
            set { m_datasheetSize = value; RaisePropertyChanged("DatasheetSize"); RaisePropertyChanged("MoreInfoDisplay"); }
        }

        private int m_datasheetPages;
        public int DatasheetPages
        {
            get { return m_datasheetPages; }
            set { m_datasheetPages = value; RaisePropertyChanged("DatasheetPages"); RaisePropertyChanged("MoreInfoDisplay"); }
        }

        private string m_manufacturerSite;
        public string ManufacturerSite
        {
            get { return m_manufacturerSite; }
            set { m_manufacturerSite = value; RaisePropertyChanged("ManufacturerSite"); RaisePropertyChanged("MoreInfoDisplay"); }
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

        public PartHandler(AllDataSheetPart part)
        {
            m_part = part;
            CheckState();
        }

        private void CheckState()
        {
            string code = m_part.Code;

            bool isDownloading;
            lock(Global.DownloadListLock) isDownloading = Global.DownloadList.ContainsKey(code);
            if (isDownloading)
            {
                lock(Global.DownloadListLock) State = Global.DownloadList[code];
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
            string code = m_part.Code;
            await Task.Run(() =>
            {
                bool contains;
                lock (Global.DownloadListLock) contains = Global.DownloadList.ContainsKey(code);
                while (contains)
                {
                    Task.Delay(100);
                    lock (Global.DownloadListLock) contains = Global.DownloadList.ContainsKey(code);
                }
            });
            CheckState();
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

            string code = m_part.Code;
            if (State == PartDatasheetState.Saved)
            {
                string pdfPath = Global.BuildSavedDatasheetPath(code);
                foreach (var item in Global.SavedParts)
                {
                    string savedCode = AllDataSheetPart.BuildCodeFromLink(item.DatasheetSiteLink, item.Name, item.Manufacturer, item.DatasheetSiteLink.GetHashCode().ToString());
                    if (savedCode == code)
                    {
                        item.LastUseDate = DateTime.Now;
                        break;
                    }
                }
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

            string code = m_part.Code;

            if (State == PartDatasheetState.NotDownloaded)
            {
                State = PartDatasheetState.Downloading;
                string pdfPath = Global.BuildSavedDatasheetPath(code);
                await DownloadPdf(pdfPath);
                CheckState();
            }

            if (State == PartDatasheetState.Cached)
            {
                File.Copy(Global.BuildCachedDatasheetPath(code), Global.BuildSavedDatasheetPath(code));
            }
            CheckState();

            Debug.Assert(State == PartDatasheetState.Saved, "Pdf is not in saved state after downloading!");

            Global.SavedParts.Add(SavedPart.FromAllDataSheetPart(m_part));
        }

        public void RemovePdf()
        {
            if (State != PartDatasheetState.Saved) throw new InvalidOperationException("Pdf is not in saved state");

            for (int i = 0; i < Global.SavedParts.Count; i++)
            {
                string code = AllDataSheetPart.BuildCodeFromLink(Global.SavedParts[i].DatasheetSiteLink, Global.SavedParts[i].Name,Global.SavedParts[i].Manufacturer,Global.SavedParts[i].DatasheetSiteLink.GetHashCode().ToString());
                if (code == this.Part.Code)
                {
                    Global.SavedParts.RemoveAt(i);
                    File.Delete(Global.BuildSavedDatasheetPath(code));
                    break;
                }
            }

            CheckState();
        }
    
        private async Task DownloadPdf(string pdfPath)
        {
            Stream stream = null;
            lock(Global.DownloadListLock) Global.DownloadList.Add(m_part.Code, State);
            try
            {
                if (m_moreInfoState != PartMoreInfoState.Available) await RequestMoreInfo();
                stream = await m_part.GetDatasheetStreamAsync(m_datasheetPdfSite);
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
                lock(Global.DownloadListLock) Global.DownloadList.Remove(m_part.Code);
            }
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
                    Stream stream = m_part.GetManufacturerImageStream();

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

            AllDataSheetPart.MoreInfo moreInfo = await m_part.RequestMoreInfoAsync();
            DatasheetPdfSite = moreInfo.PdfSite;

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
    }
}
