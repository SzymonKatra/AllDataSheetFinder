using MVVMUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
using System.Net;
using System.IO;

namespace AllDataSheetFinder
{
    public class UpdateViewModel : ObservableObject
    {
        public UpdateViewModel(string downloadLink)
        {
            m_text = Global.GetStringResource("StringSearchingUpdate");
            m_progress = 0;

            DoUpdate(downloadLink);
        }

        public Action<Action> InvokeWindow;

        private string m_text;
        public string Text
        {
            get { return m_text; }
            set { m_text = value; RaisePropertyChanged("Text"); }
        }

        private int m_progress;
        public int Progress
        {
            get { return m_progress; }
            set { m_progress = value; RaisePropertyChanged("Progress"); }
        }

        private async void DoUpdate(string link)
        {
            string zipFilePath = Global.AppDataPath + Path.DirectorySeparatorChar + Global.UpdateFile;
            string extractPath =Global.AppDataPath + Path.DirectorySeparatorChar + Global.UpdateExtractDirectory;

            HttpWebRequest request = Requests.CreateDefaultRequest(link);
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    Text = Global.GetStringResource("StringDownloadingUpdate");
                    using (FileStream file = new FileStream(zipFilePath, FileMode.Create))
                    {
                        int len;
                        byte[] buffer = new byte[4096];
                        while((len = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await file.WriteAsync(buffer, 0, len);
                            Progress = (int)((decimal)(file.Position - 1) / (decimal)response.ContentLength * 50M);
                            Text = Global.GetStringResource("StringDownloadingUpdate") + Environment.NewLine + ((file.Position - 1) / 1024).ToString() + "/" + (response.ContentLength / 1024).ToString() + " KB";
                        }
                    }
                }
            }

            Text = Global.GetStringResource("StringExtractingUpdate");
            if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);

            await Task.Run(() =>
            {
                long totalSize = 0;
                long transferred = 0;
                long oldTransferred = 0;
                ZipEntry oldEntry = null;
                using (ZipFile zip = new ZipFile(zipFilePath))
                {
                    foreach (ZipEntry item in zip.Entries) totalSize += item.UncompressedSize;

                    zip.ExtractProgress += (s, e) =>
                    {
                        if(e.CurrentEntry != oldEntry)
                        {
                            oldEntry = e.CurrentEntry;
                            oldTransferred = 0;
                        }
                        if (e.EventType == ZipProgressEventType.Extracting_EntryBytesWritten)
                        {
                            transferred += e.BytesTransferred - oldTransferred;
                            oldTransferred = e.BytesTransferred;
                        }

                        InvokeWindow.Invoke(() =>
                        {
                            Text = Global.GetStringResource("StringExtractingUpdate") + Environment.NewLine + (transferred / 1024).ToString() + "/" + (totalSize / 1024).ToString() + " KB";
                            Progress = 50 + (int)((decimal)transferred / (decimal)totalSize * 50M);
                        });
                    };
                    zip.ExtractAll(extractPath, ExtractExistingFileAction.OverwriteSilently);
                }
            });

            Global.Dialogs.Close(this);
        }
    }
}
