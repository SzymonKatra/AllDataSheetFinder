using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder
{
    public enum PartDatasheetState
    {
        NotDownloaded,
        Downloading,
        DownloadingAndOpening,
        Cached,
        Saved
    }
}
