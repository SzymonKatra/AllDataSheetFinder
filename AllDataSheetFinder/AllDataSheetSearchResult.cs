using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder
{
    public struct AllDataSheetSearchResult
    {
        public List<AllDataSheetPart> Parts { get; set; }
        public AllDataSheetSearchContext SearchContext { get; set; }
    }
}
