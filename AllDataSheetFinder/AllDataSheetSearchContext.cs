using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder
{
    public class AllDataSheetSearchContext
    {
        public AllDataSheetSearchContext(string searchValue, int totalPages)
        {
            SearchValue = searchValue;
            TotalPages = totalPages;
        }

        public string SearchValue { get; private set; }
        public string Referer { get; set; }
        public int NextPage { get; set; }
        public int TotalPages { get; private set; }
    }
}
