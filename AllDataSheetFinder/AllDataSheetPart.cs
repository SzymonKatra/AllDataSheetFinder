using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using HtmlAgilityPack;

namespace AllDataSheetFinder
{
    public class AllDataSheetPart
    {
        private string m_manufacturer;
        public string Manufacturer
        {
            get { return m_manufacturer; }
            set { m_manufacturer = value; }
        }

        private string m_manufacturerImageLink;
        public string ManufacturerImageLink
        {
            get { return m_manufacturerImageLink; }
            set { m_manufacturerImageLink = value; }
        }

        private string m_name;
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        private string m_description;
        public string Description
        {
            get { return m_description; }
            set { m_description = value; }
        }

        private string m_datasheetSiteLink;
        public string DatasheetSiteLink
        {
            get { return m_datasheetSiteLink; }
            set { m_datasheetSiteLink = value; }
        }

        public string Code
        {
            get
            {
                Uri uri = new Uri(m_datasheetSiteLink);
                string[] segments = uri.Segments;
                if (segments.Length < 3) return string.Format("{0}_{1}_{2}", m_name.Replace(' ', '-'), m_manufacturer.Replace(' ', '-'), m_datasheetSiteLink.GetHashCode().ToString());
                string nameSegment = segments[segments.Length - 1];
                string manufacturerSegment = segments[segments.Length - 2];
                string numberSegment = segments[segments.Length - 3];

                nameSegment = nameSegment.Remove(nameSegment.Length - 5); // remove ".html"
                manufacturerSegment = manufacturerSegment.Remove(manufacturerSegment.Length - 1); // remove slash
                numberSegment = numberSegment.Remove(numberSegment.Length - 1); // remove slash

                return string.Format("{0}_{1}_{2}", nameSegment, manufacturerSegment, numberSegment);
            }
        }

        private const string SiteAddress = "http://www.alldatasheet.com/view_datasheet.jsp";
        private const string SearchParameter = "Searchword";

        public static AllDataSheetSearchResult Search(string value)
        {
            string url = SiteAddress + "?" + SearchParameter + "=" + value;
            HttpWebRequest request = CreateDefaultRequest(url);
            string result = ReadResponseString(request);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(result);

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

            int totalPages = int.Parse(tokens[1]);

            AllDataSheetSearchContext searchContext = new AllDataSheetSearchContext(value, totalPages);
            searchContext.NextPage = 2;
            searchContext.Referer = url;

            AllDataSheetSearchResult ret = new AllDataSheetSearchResult();
            ret.Parts = FilterResults(document);
            ret.SearchContext = searchContext;

            return ret;
        }
        public static AllDataSheetSearchResult Search(AllDataSheetSearchContext searchContext)
        {
            if (searchContext.NextPage > searchContext.TotalPages)
            {
                AllDataSheetSearchResult ret = new AllDataSheetSearchResult();
                ret.Parts = null;
                ret.SearchContext = searchContext;
                return ret;
            }

            string url = SiteAddress + "?" + SearchParameter + "=" + searchContext.SearchValue + "&sPage=" + searchContext.NextPage;
            HttpWebRequest request = CreateDefaultRequest(url);
            request.Referer = searchContext.Referer;
            string result = ReadResponseString(request);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(result);

            searchContext.NextPage++;
            searchContext.Referer = url;

            AllDataSheetSearchResult sret = new AllDataSheetSearchResult();
            sret.Parts = FilterResults(document);
            sret.SearchContext = searchContext;

            return sret;
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

        public Stream GetDatasheetStream()
        {
            List<string> responseHeaders = new List<string>();

            HttpWebRequest request = CreateDefaultRequest(m_datasheetSiteLink);
            string result = ReadResponseString(request);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(result);

            HtmlNode rootElement = document.DocumentNode;

            IEnumerable<HtmlNode> matchingNodes = from node in rootElement.Descendants("td")
                                                  where IsAttributeValueLike(node, "width", "283") && IsAttributeValueLike(node, "height", "367") &&
                                                        IsAttributeValueLike(node, "align", "center") && IsAttributeValueLike(node, "valign", "top")
                                                  select node;

            HtmlNode aNode = matchingNodes.ElementAt(0).Element("a");

            string pdfSite = GetAttributeValueOrEmpty(aNode, "href");

            Uri pdfUri = new Uri(pdfSite);
            string pdfHost = pdfUri.GetLeftPart(UriPartial.Scheme) + pdfUri.Host;

            CookieContainer cookies = new CookieContainer();

            request = CreateDefaultRequest(pdfSite);
            request.Referer = m_datasheetSiteLink;
            request.CookieContainer = cookies;
            result = ReadResponseString(request);

            document = new HtmlDocument();
            document.LoadHtml(result);

            rootElement = document.DocumentNode;

            matchingNodes = from node in rootElement.Descendants("iframe")
                            where IsAttributeValueLike(node, "height", "810") && IsAttributeValueLike(node, "name", "333") && IsAttributeValueLike(node, "width", "100%")
                            select node;

            string pdfPath = GetAttributeValueOrEmpty(matchingNodes.ElementAt(0), "src");

            string pdfDirect = pdfHost + pdfPath;

            cookies.Add(new Uri(pdfDirect), cookies.GetCookies(new Uri(pdfSite)));

            request = CreateDefaultRequest(pdfDirect);
            request.Referer = pdfSite;
            request.CookieContainer = cookies;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.GetResponseStream();
                //byte[] buffer = new byte[4096];
                //using (BinaryReader reader = new BinaryReader(response.GetResponseStream()))
                //{
                //    int len;
                //    while ((len = reader.Read(buffer, 0, buffer.Length)) > 0)
                //    {
                //        stream.Write(buffer, 0, len);
                //    }
                //}
        }
        public Task<Stream> GetDatasheetStreamAsync()
        {
            return Task.Run(() => GetDatasheetStream());
        }

        public Stream GetManufacturerImageStream()
        {
            HttpWebRequest request = CreateDefaultRequest(m_manufacturerImageLink);
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

        private static HttpWebRequest CreateDefaultRequest(string url)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.UserAgent = "AllDataSheetFinder";
            request.Method = "GET";
            return request;
        }
        private static string ReadResponseString(HttpWebRequest request)
        {
            string result;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    result = WebUtility.HtmlDecode(reader.ReadToEnd());
                }
            }
            return result;
        }
    }
}
