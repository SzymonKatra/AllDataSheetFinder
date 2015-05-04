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
        public bool Custom { get; set; }

        public static SavedPart FromAllDataSheetPart(AllDataSheetPart part)
        {
            SavedPart result = new SavedPart();
            result.Name = part.Name;
            result.Description = part.Description;
            result.Manufacturer = part.Manufacturer;
            result.ManufacturerImageLink = part.ManufacturerImageLink;
            result.DatasheetSiteLink = part.DatasheetSiteLink;
            result.Custom = false;
            return result;
        }
        public AllDataSheetPart ToAllDataSheetPart()
        {
            AllDataSheetPart result = new AllDataSheetPart();
            result.Name = this.Name;
            result.Description = this.Description;
            result.Manufacturer = this.Manufacturer;
            result.ManufacturerImageLink = this.ManufacturerImageLink;
            result.DatasheetSiteLink = this.DatasheetSiteLink;
            return result;
        }
    }
}
