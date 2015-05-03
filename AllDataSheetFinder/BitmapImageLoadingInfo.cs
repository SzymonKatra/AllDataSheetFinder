using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AllDataSheetFinder
{
    public class BitmapImageLoadingInfo
    {
        public BitmapImage Image;
        public bool Loaded;
        public bool Loading;

        public static BitmapImageLoadingInfo CreateDefault()
        {
            BitmapImageLoadingInfo result = new BitmapImageLoadingInfo();
            result.Image = new BitmapImage();
            result.Loaded = false;
            result.Loading = false;
            return result;
        }
    }
}
