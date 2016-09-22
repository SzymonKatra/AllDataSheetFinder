using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using HtmlAgilityPack;

namespace AllDataSheetFinder
{
    public class AllDataSheetPart
    {
        public struct MoreInfo
        {
            public string PdfSite { get; set; }
            public string Size { get; set; }
            public string Pages { get; set; }
            public string ManufacturerSite { get; set; }
        }

        protected AllDataSheetPart()
        {
        }
        public AllDataSheetPart(string datasheetSiteLink)
        {
            m_datasheetSiteLink = datasheetSiteLink;
            m_onlyContext = true;
        }

        private string m_manufacturer;
        public string Manufacturer
        {
            get
            {
                if (m_onlyContext) throw new InvalidOperationException("AllDataSheetPart is in OnlyContext state");
                return m_manufacturer;
            }
        }

        private string m_manufacturerImageLink;
        public string ManufacturerImageLink
        {
            get
            {
                if (m_onlyContext) throw new InvalidOperationException("AllDataSheetPart is in OnlyContext state");
                return m_manufacturerImageLink;
            }
        }

        private string m_name;
        public string Name
        {
            get
            {
                if (m_onlyContext) throw new InvalidOperationException("AllDataSheetPart is in OnlyContext state");
                return m_name;
            }
        }

        private string m_description;
        public string Description
        {
            get
            {
                if (m_onlyContext) throw new InvalidOperationException("AllDataSheetPart is in OnlyContext state");
                return m_description;
            }
        }

        private string m_datasheetSiteLink;
        public string DatasheetSiteLink
        {
            get { return m_datasheetSiteLink; }
        }

        private bool m_onlyContext = false;
        public bool OnlyContext
        {
            get { return m_onlyContext; }
        }

        private const string SiteAddress = "http://www.alldatasheet.com/view_datasheet.jsp";

        public static AllDataSheetSearchResult Search(string value)
        {
            string url = $"{SiteAddress}?Searchword={value}&sPage=1&sField=4";
            HttpWebRequest request = Requests.CreateDefaultRequest(url);
            string result = Requests.ReadResponseString(request);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(result);

            int totalPages;
            AllDataSheetSearchContext.SearchOption option;
            FilterSearchMode(document, out totalPages, out option);

            AllDataSheetSearchContext searchContext = new AllDataSheetSearchContext(value);
            searchContext.NextPage = 1;
            searchContext.TotalPages = totalPages;
            if (option != AllDataSheetSearchContext.SearchOption.Match)
            {
                searchContext.UsedOptions.Add(AllDataSheetSearchContext.SearchOption.Match);
                searchContext.Option = option;
            }
            searchContext.Next(url);          

            AllDataSheetSearchResult ret = new AllDataSheetSearchResult();
            ret.Parts = FilterResults(document);
            ret.SearchContext = searchContext;

            return ret;
        }
        public static AllDataSheetSearchResult Search(AllDataSheetSearchContext searchContext)
        {
            if (!searchContext.CanLoadMore)
            {
                AllDataSheetSearchResult ret = new AllDataSheetSearchResult();
                ret.Parts = null;
                ret.SearchContext = searchContext;
                return ret;
            }

            while (true)
            {
                string url = $"{SiteAddress}?Searchword={searchContext.SearchValue}&sPage={searchContext.NextPage}&sField={(int)searchContext.Option}";
                HttpWebRequest request = Requests.CreateDefaultRequest(url);
                request.Referer = searchContext.Referer;
                string result = Requests.ReadResponseString(request);

                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(result);

                if (searchContext.NextPage == 1)
                {
                    int totalPages;
                    AllDataSheetSearchContext.SearchOption option;
                    FilterSearchMode(document, out totalPages, out option);
                    searchContext.TotalPages = totalPages;
                    if (searchContext.Option != option)
                    {
                        if (!searchContext.UsedOptions.Contains(searchContext.Option)) searchContext.UsedOptions.Add(searchContext.Option);
                        searchContext.Option = option;
                    }
                }

                AllDataSheetSearchResult sret = new AllDataSheetSearchResult();
                if (searchContext.UsedOptions.Contains(searchContext.Option))
                {
                    sret.Parts = new List<AllDataSheetPart>();
                    if (searchContext.CanLoadMore)
                    {
                        searchContext.Next(url);
                        continue;
                    }
                }
                else
                {
                    sret.Parts = FilterResults(document);
                }

                sret.SearchContext = searchContext;
                searchContext.Next(url);

                return sret;
            }
        }
        public static Task<AllDataSheetSearchResult> SearchAsync(string value)
        {
            return Task.Run(() => Search(value));
        }
        public static Task<AllDataSheetSearchResult> SearchAsync(AllDataSheetSearchContext searchContext)
        {
            return Task.Run(() => Search(searchContext));
        }

        private static List<AllDataSheetPart> FilterResults(HtmlDocument document)
        {
            HtmlNode rootNode = document.DocumentNode;
            HtmlNode htmlNode = rootNode.Element("html");
            HtmlNode bodyNode = htmlNode.Element("body");

            IEnumerable<HtmlNode> tableWithElementsNodes = from node in bodyNode.Descendants("table")
                                                           where IsAttributeValueLike(node, "width", "100%") && IsAttributeValueLike(node, "height", "100%") &&
                                                                 IsAttributeValueLike(node, "border", "0") && IsAttributeValueLike(node, "bgcolor", "#cccccc") &&
                                                                 IsAttributeValueLike(node, "cellpadding", "1") && IsAttributeValueLike(node, "cellspacing", "1") &&
                                                                 IsAttributeValueLike(node, "class", "main") && IsAttributeValueLike(node, "align", "center")
                                                           select node;

            HtmlNode tableNode = tableWithElementsNodes.ElementAt(0);

            IEnumerable<HtmlNode> tableRows = from node in tableNode.Elements("tr") where IsAttributeValueLike(node, "class", "nv_td") select node;

            List<AllDataSheetPart> parts = new List<AllDataSheetPart>();

            AllDataSheetPart previous = new AllDataSheetPart();

            foreach (HtmlNode row in tableRows)
            {
                AllDataSheetPart part = new AllDataSheetPart();

                IEnumerable<HtmlNode> columns = row.Elements("td");
                int columnsCount = columns.Count();
                if (columnsCount < 3) continue;
                int offset = 0;

                if (columnsCount == 3)
                {
                    offset = -1;
                    part.m_manufacturer = previous.m_manufacturer;
                    part.m_manufacturerImageLink = previous.m_manufacturerImageLink;
                }
                else
                {
                    HtmlNode imgNode = columns.ElementAt(0).Element("img");
                    if (imgNode != null)
                    {
                        part.m_manufacturer = GetAttributeValueOrEmpty(imgNode, "alt");
                        part.m_manufacturerImageLink = GetAttributeValueOrEmpty(imgNode, "src");
                    }
                }

                HtmlNode secondColumn = columns.ElementAt(1 + offset);
                HtmlNode aNode = secondColumn.Element("a");
                if (aNode != null)
                {
                    part.m_datasheetSiteLink = GetAttributeValueOrEmpty(aNode, "href");
                    part.m_name = aNode.InnerText.Replace("\n", "");
                }

                HtmlNode descriptionNode = columns.ElementAt(3 + offset);
                if (descriptionNode != null)
                {
                    part.m_description = descriptionNode.InnerText;
                }

                previous = part;
                parts.Add(part);
            }
            return parts;
        }
        private static void FilterSearchMode(HtmlDocument document, out int totalPages, out AllDataSheetSearchContext.SearchOption option)
        {
            var matchingTables = from node in document.DocumentNode.Descendants("table")
                                 where IsAttributeValueLike(node, "width", "95%") && IsAttributeValueLike(node, "border", "0") &&
                                       IsAttributeValueLike(node, "cellpadding", "0") && IsAttributeValueLike(node, "cellspacing", "0") &&
                                       IsAttributeValueLike(node, "class", "main") && IsAttributeValueLike(node, "align", "center")
                                 select node;

            HtmlNode tdNode = matchingTables.ElementAt(1).Element("tr").Element("td");
            List<char> text = tdNode.InnerText.Substring(tdNode.InnerText.LastIndexOf('(')).ToList();
            text.RemoveAll(x => !char.IsDigit(x) && x != '/');
            string pages = string.Concat(text); // current/total
            string[] tokens = pages.Split('/');

            totalPages = int.Parse(tokens[1]);
            string upperInnerText = tdNode.InnerText.ToUpper();
            option = AllDataSheetSearchContext.SearchOption.Match;
            if (upperInnerText.Contains("START")) option = AllDataSheetSearchContext.SearchOption.StartsWith;
            else if (upperInnerText.Contains("END")) option = AllDataSheetSearchContext.SearchOption.EndsWith;
            else if (upperInnerText.Contains("INCLUDED")) option = AllDataSheetSearchContext.SearchOption.Included;
        }

        public MoreInfo RequestMoreInfo()
        {
            HttpWebRequest request = Requests.CreateDefaultRequest(m_datasheetSiteLink);
            string result = Requests.ReadResponseString(request);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(result);

            HtmlNode rootElement = document.DocumentNode;

            IEnumerable<HtmlNode> matchingNodes = from node in rootElement.Descendants("td")
                                                  where IsAttributeValueLike(node, "width", "283") && IsAttributeValueLike(node, "height", "367") &&
                                                        IsAttributeValueLike(node, "align", "center") && IsAttributeValueLike(node, "valign", "top")
                                                  select node;

            HtmlNode aNode = matchingNodes.ElementAt(0).Element("a");

            MoreInfo moreInfo = new MoreInfo();

            moreInfo.PdfSite = GetAttributeValueOrEmpty(aNode, "href");

            matchingNodes = from node in rootElement.Descendants("td")
                            where IsAttributeValueLike(node, "height", "40") && IsAttributeValueLike(node, "class", "gray_title") &&
                                  IsAttributeValueLike(node, "width", "88")
                            select node;

            HtmlNode sizeNode = matchingNodes.ElementAt(2).ParentNode;
            moreInfo.Size = sizeNode.Elements("td").ElementAt(1).Element("font").InnerText;

            HtmlNode pagesNode = matchingNodes.ElementAt(3).ParentNode;
            moreInfo.Pages = pagesNode.Elements("td").ElementAt(1).Element("font").InnerText;

            HtmlNode manufacturerSiteNode = matchingNodes.ElementAt(5).ParentNode;
            moreInfo.ManufacturerSite = manufacturerSiteNode.Elements("td").ElementAt(1).InnerText;

            return moreInfo;
        }
        public Task<MoreInfo> RequestMoreInfoAsync()
        {
            return Task.Run(() => RequestMoreInfo());
        }

        public Stream GetDatasheetStream()
        {
            MoreInfo moreInfo = RequestMoreInfo();
            return GetDatasheetStream(moreInfo.PdfSite);
        }
        public Stream GetDatasheetStream(string pdfSite)
        {
            Uri pdfUri = new Uri(pdfSite);
            string pdfHost = pdfUri.GetLeftPart(UriPartial.Scheme) + pdfUri.Host;

            CookieContainer cookies = new CookieContainer();

            HttpWebRequest request = Requests.CreateDefaultRequest(pdfSite);
            request.Referer = m_datasheetSiteLink;
            request.CookieContainer = cookies;
            string result = Requests.ReadResponseString(request);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(result);

            HtmlNode rootElement = document.DocumentNode;

            IEnumerable<HtmlNode> matchingNodes = from node in rootElement.Descendants("iframe")
                            where IsAttributeValueLike(node, "height", "810") && IsAttributeValueLike(node, "name", "333") && IsAttributeValueLike(node, "width", "100%")
                            select node;

            string pdfPath = GetAttributeValueOrEmpty(matchingNodes.ElementAt(0), "src");

            string pdfDirect = pdfHost + pdfPath;

            cookies.Add(new Uri(pdfDirect), cookies.GetCookies(new Uri(pdfSite)));

            request = Requests.CreateDefaultRequest(pdfDirect);
            request.Referer = pdfSite;
            request.CookieContainer = cookies;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.GetResponseStream();
        }
        public Task<Stream> GetDatasheetStreamAsync()
        {
            return Task.Run(() => GetDatasheetStream());
        }
        public Task<Stream> GetDatasheetStreamAsync(string pdfSite)
        {
            return Task.Run(() => GetDatasheetStream(pdfSite));
        }

        public Stream GetManufacturerImageStream()
        {
            HttpWebRequest request = Requests.CreateDefaultRequest(m_manufacturerImageLink);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.GetResponseStream();
        }
        public Task<Stream> GetManufacturerImageStreamAsync()
        {
            return Task.Run(() => GetManufacturerImageStream());
        }

        private static bool IsAttributeValueLike(HtmlNode node, string name, string value)
        {
            if (!node.Attributes.Contains(name)) return false;
            return node.Attributes[name].Value.Contains(value);
        }
        private static string GetAttributeValueOrEmpty(HtmlNode node, string name)
        {
            if (!node.Attributes.Contains(name)) return string.Empty;
            return node.Attributes[name].Value;
        }
    }
}
