using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVMUtils;
using System.IO;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Diagnostics;
using MVVMUtils.Collections;
using System.Text.RegularExpressions;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace AllDataSheetFinder
{
    public class PartViewModel : DataViewModelBase<PartViewModel, Part>
    {
        public PartViewModel()
            : this(new Part(), false)
        {
        }
        public PartViewModel(Part model, bool modelValid = true)
            : base(model)
        {
            m_tags = new SynchronizedPerItemObservableCollection<ValueViewModel<string>, string>(model.Tags, x => new ValueViewModel<string>(x));
            m_tags.CollectionChanged += (s, e) => RaisePropertyChanged("MoreInfoDisplay");
            m_tags.ItemPropertyInCollectionChanged += (s, e) => RaisePropertyChanged("MoreInfoDisplay");

            m_moreInfoState = PartMoreInfoState.Available;
            if (modelValid)
            {
                if (!model.Custom) MakeContext();
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
            result.RebuildTags();
            result.Context = part;
            result.MoreInfoState = PartMoreInfoState.NotAvailable;
            result.CheckState();
            return result;
        }
        public static PartViewModel MakeCustom(string originalPath)
        {
            string savedDirectory = Global.AppDataPath + Path.DirectorySeparatorChar + Global.SavedDatasheetsDirectory;
            string fileName = Path.GetFileNameWithoutExtension(originalPath);
            string resultFilePath = Global.BuildSavedDatasheetPath(fileName);
            int count = 1;
            while (File.Exists(resultFilePath))
            {
                resultFilePath = Global.BuildSavedDatasheetPath(fileName + '(' + count.ToString() + ')');
                count++;
            }

            PartViewModel result = new PartViewModel();
            result.Description = fileName;
            result.RebuildTags();
            result.Custom = true;
            result.CustomPath = resultFilePath;
            result.DatasheetSize = (new FileInfo(originalPath)).Length;
            result.CheckState();

            return result;
        }

        private static object s_downloadListLock = new object();
        //private static Dictionary<string, PartDatasheetState> s_downloadList = new Dictionary<string, PartDatasheetState>();
        private static Dictionary<string, PartViewModel> s_downloadList = new Dictionary<string, PartViewModel>();

        public string Name
        {
            get { return Model.Name; }
            set { Model.Name = value; RaisePropertyChanged("Name"); RaisePropertyChanged("Code"); }
        }
        public string Description
        {
            get { return Model.Description; }
            set { Model.Description = value; RaisePropertyChanged("Description"); }
        }
        public string Manufacturer
        {
            get { return Model.Manufacturer; }
            set { Model.Manufacturer = value; RaisePropertyChanged("Manufacturer"); RaisePropertyChanged("Code"); }
        }
        public string ManufacturerImageLink
        {
            get { return Model.ManufacturerImageLink; }
            set { Model.ManufacturerImageLink = value; RaisePropertyChanged("ManufacturerImageLink"); RaisePropertyChanged("ImageFileName"); }
        }
        public string DatasheetSiteLink
        {
            get { return Model.DatasheetSiteLink; }
            set { Model.DatasheetSiteLink = value; RaisePropertyChanged("DatasheetSiteLink"); RaisePropertyChanged("Code"); }
        }
        public string DatasheetPdfLink
        {
            get { return Model.DatasheetPdfLink; }
            set { Model.DatasheetPdfLink = value; RaisePropertyChanged("DatasheetPdfLink"); }
        }
        public long DatasheetSize
        {
            get { return Model.DatasheetSize; }
            set { Model.DatasheetSize = value; RaisePropertyChanged("DatasheetSize"); RaisePropertyChanged("MoreInfoDisplay"); }
        }
        public int DatasheetPages
        {
            get { return Model.DatasheetPages; }
            set { Model.DatasheetPages = value; RaisePropertyChanged("DatasheetPages"); RaisePropertyChanged("MoreInfoDisplay"); }
        }
        public string ManufacturerSite
        {
            get { return Model.ManufacturerSite; }
            set { Model.ManufacturerSite = value; RaisePropertyChanged("ManufacturerSite"); RaisePropertyChanged("MoreInfoDisplay"); }
        }
        public DateTime LastUseDate
        {
            get { return Model.LastUseDate; }
            set { Model.LastUseDate = value; RaisePropertyChanged("LastUseDate"); }
        }
        public bool Custom
        {
            get { return Model.Custom; }
            set { Model.Custom = value; RaisePropertyChanged("Custom"); }
        }
        public string CustomPath
        {
            get { return Model.CustomPath; }
            set { Model.CustomPath = value; RaisePropertyChanged("CustomPath"); }
        }

        private SynchronizedPerItemObservableCollection<ValueViewModel<string>, string> m_tags;
        public SynchronizedPerItemObservableCollection<ValueViewModel<string>, string> Tags
        {
            get { return m_tags; }
        }

        private const int MaxTagsDisplay = 10;

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
                StringBuilder tagsBuilder = new StringBuilder();
                int limit = Math.Min(Tags.Count, MaxTagsDisplay);
                for (int i = 0; i < limit; i++)
                {
                    tagsBuilder.Append(Tags[i].Value);
                    if (i != limit - 1) tagsBuilder.Append(", ");
                }
                if (Tags.Count > MaxTagsDisplay) tagsBuilder.Append(", ...");

                return string.Format(@"{1}: {2} KB{0}{3}: {4}{0}{5}: {6}{0}{7}: {8}",
                                     Environment.NewLine,
                                     Global.GetStringResource("StringSize"), DatasheetSize / 1024,
                                     Global.GetStringResource("StringPages"), DatasheetPages,
                                     Global.GetStringResource("StringManufacturerSite"), ManufacturerSite,
                                     Global.GetStringResource("StringTags"), tagsBuilder.ToString());
            }
        }

        public string Code
        {
            get
            {
                if (DatasheetSiteLink == null) return string.Empty;
                return BuildCodeFromLink(DatasheetSiteLink, Name, Manufacturer, DatasheetSiteLink.GetHashCode().ToString());
            }
        }
        public string ImageFileName
        {
            get
            {
                if (ManufacturerImageLink == null) return string.Empty;
                Uri imageUri = new Uri(ManufacturerImageLink);
                if (imageUri.Segments.Length <= 0) return string.Empty;
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

        private decimal m_progress = 1M;
        public decimal Progress
        {
            get { return m_progress; }
            private set { m_progress = value; RaisePropertyChanged("Progress"); }
        }

        public async void LoadImage()
        {
            string file = ImageFileName;
            if (string.IsNullOrWhiteSpace(file)) return;
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
                byte[] imageData = File.ReadAllBytes(imagePath);
                MemoryStream stream = new MemoryStream(imageData);
                info.Image.BeginInit();
                info.Image.CacheOption = BitmapCacheOption.OnLoad;
                info.Image.StreamSource = stream;
                info.Image.EndInit();
            }
            else if (!Custom && !m_context.OnlyContext)
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
                info.Image.CacheOption = BitmapCacheOption.OnLoad;
                info.Image.StreamSource = memory;
                info.Image.EndInit();
            }

            this.Image = info.Image;

            info.Loaded = true;
            info.Loading = false;
        }
        public async Task RequestMoreInfo()
        {
            if (Custom) throw new InvalidOperationException("Part is custom");

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
            if (Custom)
            {
                LastUseDate = DateTime.Now;
                Process.Start(CustomPath);
                return;
            }

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
                LastUseDate = DateTime.Now;
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
            if (Custom) throw new InvalidOperationException("Part is custom");

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

            if (MoreInfoState == PartMoreInfoState.NotAvailable) await RequestMoreInfo();

            Debug.Assert(State == PartDatasheetState.Saved, "Pdf is not in saved state after downloading!");

            LastUseDate = DateTime.Now;
        }
        public void RemovePdf()
        {
            if (State != PartDatasheetState.Saved) throw new InvalidOperationException("Pdf is not in saved state");

            string path = (Custom ? CustomPath : Global.BuildSavedDatasheetPath(Code));
            File.Delete(path);

            CheckState();
        }

        private async Task DownloadPdf(string pdfPath)
        {
            Stream stream = null;
            lock (s_downloadListLock) s_downloadList.Add(Code, this);
            try
            {
                if (m_moreInfoState != PartMoreInfoState.Available) await RequestMoreInfo();
                stream = await m_context.GetDatasheetStreamAsync(DatasheetPdfLink);
                Progress = 0M;
                using (FileStream file = new FileStream(pdfPath, FileMode.Create))
                {
                    byte[] buffer = new byte[4096];
                    int len;
                    long written = 0;
                    
                    while ((len = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await file.WriteAsync(buffer, 0, len);
                        written += len;
                        Progress = (decimal)written / (decimal)DatasheetSize;
                    }
                }
                PdfDocument document = PdfReader.Open(pdfPath);
                document.Close();
                document.Info.Title = this.Name;
                document.Save(pdfPath);
                Progress = 1M;
            }
            finally
            {
                if (stream != null) stream.Close();
                lock (s_downloadListLock) s_downloadList.Remove(Code);
            }
        }

        public void CheckState()
        {
            if (Custom)
            {
                State = PartDatasheetState.Saved;
                return;
            }

            string code = Code;

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
        public void RebuildTags()
        {
            Tags.Clear();
            string[] tokens = Description.Split(' ');
            foreach (var item in tokens)
            {
                string toAdd = item.RemoveAll(x => char.IsWhiteSpace(x) || x == ',');
                if (string.IsNullOrWhiteSpace(toAdd)) continue;
                Tags.Add(new ValueViewModel<string>(toAdd));
            }
        }
        public Task ComputePagesCount()
        {
            if (!Custom) throw new InvalidOperationException("Part must be custom");
            return Task.Run(() =>
            {
                PdfDocument document = null;
                try
                {
                    document = PdfReader.Open(CustomPath);
                    this.DatasheetPages = document.PageCount;
                }
                catch
                {
                    // if PdfReader can't open pdf file, try using regex method to count pages
                    string fileContent = File.ReadAllText(CustomPath);
                    Regex regex = new Regex(@"Type\/Page[^s]");
                    this.DatasheetPages = regex.Matches(fileContent).Count;
                }
                finally
                {
                    if (document != null) document.Close();
                }
            });
        }

        public static bool GetDownloadingIfExists(string code, ref PartViewModel part)
        {
            lock (s_downloadListLock)
            {
                if (s_downloadList.ContainsKey(code))
                {
                    part = s_downloadList[code];
                    return true;
                }
            }
            return false;
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

        protected override void OnPopCopy(WorkingCopyResult result)
        {
            ObjectsPack pack = CopyStack.Pop();
            if (result == WorkingCopyResult.Restore)
            {
                this.Name = (string)pack.Read();
                this.Description = (string)pack.Read();
                this.Manufacturer = (string)pack.Read();
                this.ManufacturerImageLink = (string)pack.Read();
                this.DatasheetSiteLink = (string)pack.Read();
                this.DatasheetPdfLink = (string)pack.Read();
                this.DatasheetPages = (int)pack.Read();
                this.DatasheetSize = (long)pack.Read();
                this.ManufacturerSite = (string)pack.Read();
                this.LastUseDate = (DateTime)pack.Read();
                this.Custom = (bool)pack.Read();
                this.CustomPath = (string)pack.Read();
            }

            Tags.PopCopy(result);
        }
        protected override void OnPushCopy()
        {
            ObjectsPack pack = new ObjectsPack();
            pack.Write(this.Name);
            pack.Write(this.Description);
            pack.Write(this.Manufacturer);
            pack.Write(this.ManufacturerImageLink);
            pack.Write(this.DatasheetSiteLink);
            pack.Write(this.DatasheetPdfLink);
            pack.Write(this.DatasheetPages);
            pack.Write(this.DatasheetSize);
            pack.Write(this.ManufacturerSite);
            pack.Write(this.LastUseDate);
            pack.Write(this.Custom);
            pack.Write(this.CustomPath);

            CopyStack.Push(pack);

            Tags.PushCopy();
        }
    }
}
