using System.Collections.Generic;

namespace AllDataSheetFinder
{
    public struct AllDataSheetSearchResult
    {
        public List<AllDataSheetPart> Parts { get; set; }
        public AllDataSheetSearchContext SearchContext { get; set; }
    }
}
