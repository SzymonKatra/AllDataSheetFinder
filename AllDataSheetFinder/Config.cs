using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using MVVMUtils;

namespace AllDataSheetFinder
{
    [Serializable]
    public class Config : ObservableObject
    {
        public long MaxDatasheetsCacheSize { get; set; } = 100 * 1024 * 1024; // 100 MiB
        public string Language { get; set; } = string.Empty;
        public bool FavouritesOnStart { get; set; } = false;
        [IgnoreDataMember]
        private bool m_enableSmoothScrolling = true;
        public bool EnableSmoothScrolling
        {
            get { return m_enableSmoothScrolling; }
            set { m_enableSmoothScrolling = value; RaisePropertyChanged("EnableSmoothScrolling"); }
        }

        public void ApplyFromOther(Config other)
        {
            this.MaxDatasheetsCacheSize = other.MaxDatasheetsCacheSize;
            this.Language = other.Language;
            this.FavouritesOnStart = other.FavouritesOnStart;
            this.EnableSmoothScrolling = other.EnableSmoothScrolling;
        }
    }
}
