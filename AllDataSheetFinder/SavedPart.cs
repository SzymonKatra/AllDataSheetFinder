using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder
{
    [Serializable]
    public class SavedPart
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

        public static SavedPart FromPartHandler(PartHandler part)
        {
            SavedPart result = new SavedPart();
            result.Name = part.Part.Name;
            result.Description = part.Part.Description;
            result.Manufacturer = part.Part.Manufacturer;
            result.ManufacturerImageLink = part.Part.ManufacturerImageLink;
            result.DatasheetSiteLink = part.Part.DatasheetSiteLink;
            result.DatasheetPdfLink = part.DatasheetPdfSite;
            result.DatasheetSize = part.DatasheetSize;
            result.DatasheetPages = part.DatasheetPages;
            result.ManufacturerSite = part.ManufacturerSite;
            result.Custom = false;
            return result;
        }
        public PartHandler ToPartHandler()
        {
            AllDataSheetPart part = new AllDataSheetPart();
            part.Name = this.Name;
            part.Description = this.Description;
            part.Manufacturer = this.Manufacturer;
            part.ManufacturerImageLink = this.ManufacturerImageLink;
            part.DatasheetSiteLink = this.DatasheetSiteLink;
            PartHandler result = new PartHandler(part);           
            result.DatasheetPdfSite = this.DatasheetPdfLink;
            result.DatasheetSize = this.DatasheetSize;
            result.DatasheetPages = this.DatasheetPages;
            result.ManufacturerSite = this.ManufacturerSite;
            result.MoreInfoState = PartMoreInfoState.Available;
            return result;
        }
    }
}
