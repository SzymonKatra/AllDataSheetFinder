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

        public string ImageFileName
        {
            get
            {
                Uri imageUri = new Uri(m_part.ManufacturerImageLink);
                return imageUri.Segments[imageUri.Segments.Length - 1];
            }
        }

        public PartHandler(AllDataSheetPart part)
        {
            m_part = part;
        }

        public async Task OpenPdf()
        {
            string code = m_part.Code;
            string pdfPath = Global.BuildSavedDatasheetPath(code);
            if (File.Exists(pdfPath))
            {
                Process.Start(pdfPath);
                return;
            }

            pdfPath = Global.BuildCachedDatasheetPath(code);
            if (File.Exists(pdfPath))
            {
                Process.Start(pdfPath);
                return;
            }
            else
            {
                Stream stream = null;
                try
                {
                    stream = await m_part.GetDatasheetStreamAsync();
                    await Task.Run(() =>
                    {
                        using (FileStream file = new FileStream(pdfPath, FileMode.OpenOrCreate))
                        {
                            byte[] buffer = new byte[4096];
                            int len;
                            while ((len = stream.Read(buffer, 0, buffer.Length)) > 0) file.Write(buffer, 0, len);
                        }
                    });
                    Process.Start(pdfPath);
                }
                finally
                {
                    if (stream != null) stream.Close();
                }
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

                    FileStream fileStream = new FileStream(imagePath, FileMode.OpenOrCreate);
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
    }
}
