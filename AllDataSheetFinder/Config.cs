﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder
{
    [Serializable]
    public class Config
    {
        public long MaxDatasheetsCacheSize { get; set; }
        public string Language { get; set; }
        public bool FavouritesOnStart { get; set; }
    }
}
