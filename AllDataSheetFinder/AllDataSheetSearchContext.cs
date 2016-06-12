using System.Collections.Generic;

namespace AllDataSheetFinder
{
    public class AllDataSheetSearchContext
    {
        public enum SearchOption
        {
            Match = 4,
            StartsWith = 2,
            EndsWith = 3,
            Included = 1
        }

        public AllDataSheetSearchContext(string searchValue)
        {
            SearchValue = searchValue;
            NextPage = 1;
            Option = SearchOption.Match;
        }

        public string SearchValue { get; private set; }
        public string Referer { get; set; }
        public int NextPage { get; set; }
        public int TotalPages { get; set; }
        public SearchOption Option { get; set; }

        private List<SearchOption> m_usedOptions = new List<SearchOption>();
        public List<SearchOption> UsedOptions
        {
            get { return m_usedOptions; }
        }

        public bool CanLoadMore
        {
            get { return m_usedOptions.Count < 4; }
        }

        public void Next(string referer)
        {
            Referer = referer;
            if (NextPage < TotalPages)
            {
                NextPage++;
            }
            else
            {
                if (!m_usedOptions.Contains(Option)) m_usedOptions.Add(Option);
                if (CanLoadMore)
                {
                    while (m_usedOptions.Contains(Option))
                    {
                        switch (Option)
                        {
                            case SearchOption.Match: Option = SearchOption.StartsWith; break;
                            case SearchOption.StartsWith: Option = SearchOption.Included; break;
                            case SearchOption.Included: Option = SearchOption.EndsWith; break;
                            case SearchOption.EndsWith: Option = SearchOption.Match; break;
                        }
                    }
                }
                NextPage = 1;
            }
        }

        public void MarkAsCannotLoadMore()
        {
            if (!m_usedOptions.Contains(SearchOption.EndsWith)) m_usedOptions.Add(SearchOption.EndsWith);
            if (!m_usedOptions.Contains(SearchOption.Included)) m_usedOptions.Add(SearchOption.Included);
            if (!m_usedOptions.Contains(SearchOption.Match)) m_usedOptions.Add(SearchOption.Match);
            if (!m_usedOptions.Contains(SearchOption.StartsWith)) m_usedOptions.Add(SearchOption.StartsWith);
        }
    }
}
