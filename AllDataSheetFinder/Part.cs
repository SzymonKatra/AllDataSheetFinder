using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder
{
    [Serializable]
    public class Part
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string ManufacturerImageLink { get; set; }
        public string DatasheetSiteLink { get; set; }
        public string DatasheetPdfLink { get; set; }
        public long DatasheetSize { get; set; }
        public int DatasheetPages { get; set; }
        public string ManufacturerSite { get; set; }

        public DateTime LastUseDate { get; set; }
        public bool Custom { get; set; }
        public string CustomPath { get; set; }
    }
}
