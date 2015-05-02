using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using HtmlAgilityPack;

namespace AllDataSheetFinder.SiteParser
{
    public static class SiteActions
    {
        private const string SiteAddress = "http://www.alldatasheet.com/view.jsp";
        private const string SearchParameter = "Searchword";

        public static List<PartInfo> Search(string value)//, IProgress<int> progress)
        {
            string result = ReadResponseString(CreateDefaultRequest(SiteAddress + "?" + SearchParameter + "=" + value));

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(result);

            HtmlNode rootNode = document.DocumentNode;
            if (rootNode == null) return null;
            HtmlNode htmlNode = rootNode.Element("html");
            if (htmlNode == null) return null;
            HtmlNode bodyNode = htmlNode.Element("body");
            if (bodyNode == null) return null;

            IEnumerable<HtmlNode> tableWithElementsNodes = from node in bodyNode.Descendants("table")
                                                           where IsAttributeValueLike(node, "width", "100%") && IsAttributeValueLike(node, "height", "100%") &&
                                                                 IsAttributeValueLike(node, "border", "0") && IsAttributeValueLike(node, "bgcolor", "#cccccc") &&
                                                                 IsAttributeValueLike(node, "cellpadding", "1") && IsAttributeValueLike(node, "cellspacing", "1") &&
                                                                 IsAttributeValueLike(node, "class", "main") && IsAttributeValueLike(node, "align", "center")
                                                           select node;

            if (tableWithElementsNodes.Count() != 1) return null;
            HtmlNode tableNode = tableWithElementsNodes.ElementAt(0);

            IEnumerable<HtmlNode> tableRows = from node in tableNode.Elements("tr") where IsAttributeValueLike(node, "class", "nv_td") select node;

            List<PartInfo> parts = new List<PartInfo>();

            PartInfo previous = new PartInfo();

            foreach (HtmlNode row in tableRows)
            {
                PartInfo part = new PartInfo();

                IEnumerable<HtmlNode> columns = row.Elements("td");
                int columnsCount = columns.Count();
                if (columnsCount < 3) continue;
                int offset = 0;

                if (columnsCount == 3)
                {
                    offset = -1;
                    part.Manufacturer = previous.Manufacturer;
                    part.ManufacturerImageLink = previous.ManufacturerImageLink;
                }
                else
                {
                    HtmlNode imgNode = columns.ElementAt(0).Element("img");
                    if (imgNode != null)
                    {
                        part.Manufacturer = GetAttributeValueOrEmpty(imgNode, "alt");
                        part.ManufacturerImageLink = GetAttributeValueOrEmpty(imgNode, "src");
                    }
                }

                HtmlNode secondColumn = columns.ElementAt(1 + offset);
                HtmlNode aNode = secondColumn.Element("a");
                if (aNode != null)
                {
                    part.DatasheetSiteLink = GetAttributeValueOrEmpty(aNode, "href");
                    part.PartName = aNode.InnerText.Replace("\n", "");
                }          

                HtmlNode descriptionNode = columns.ElementAt(3 + offset);
                if (descriptionNode != null)
                {
                    part.PartDescription = descriptionNode.InnerText;
                }

                previous = part;
                parts.Add(part);
            }

            return parts;
        }

        public static bool FindDatasheetDirectLink(ref PartInfo part)
        {
            List<string> responseHeaders = new List<string>();

            HttpWebRequest request = CreateDefaultRequest(part.DatasheetSiteLink);
            string result = ReadResponseString(request);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(result);

            HtmlNode rootElement = document.DocumentNode;

            IEnumerable<HtmlNode> matchingNodes = from node in rootElement.Descendants("td")
                                                  where IsAttributeValueLike(node, "width", "283") && IsAttributeValueLike(node, "height", "367") &&
                                                        IsAttributeValueLike(node, "align", "center") && IsAttributeValueLike(node, "valign", "top")
                                                  select node;

            if (matchingNodes.Count() != 1) return false;

            HtmlNode aNode = matchingNodes.ElementAt(0).Element("a");
            if (aNode == null) return false;

            part.DatasheetPdfSiteLink = GetAttributeValueOrEmpty(aNode, "href");
            if (string.IsNullOrWhiteSpace(part.DatasheetPdfSiteLink)) return false;

            Uri pdfUri = new Uri(part.DatasheetPdfSiteLink);
            string pdfHost = pdfUri.GetLeftPart(UriPartial.Scheme) + pdfUri.Host;

            CookieContainer cookies = new CookieContainer();

            request = CreateDefaultRequest(part.DatasheetPdfSiteLink);
            request.Referer = part.DatasheetSiteLink;
            request.CookieContainer = cookies;
            result = ReadResponseString(request);

            document = new HtmlDocument();
            document.LoadHtml(result);

            rootElement = document.DocumentNode;

            matchingNodes = from node in rootElement.Descendants("iframe")
                            where IsAttributeValueLike(node, "height", "810") && IsAttributeValueLike(node, "name", "333") && IsAttributeValueLike(node, "width", "100%")
                            select node;

            if (matchingNodes.Count() != 1) return false;
            string pdfPath = GetAttributeValueOrEmpty(matchingNodes.ElementAt(0), "src");

            part.DirectDatasheetLink = pdfHost + pdfPath;

            cookies.Add(new Uri(part.DirectDatasheetLink), cookies.GetCookies(new Uri(part.DatasheetPdfSiteLink)));

            request = CreateDefaultRequest(part.DirectDatasheetLink);
            request.Referer = part.DatasheetPdfSiteLink;
            request.CookieContainer = cookies;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            byte[] buffer = new byte[4096];
            using (BinaryReader reader = new BinaryReader(response.GetResponseStream()))
            {
                using (FileStream file = new FileStream(@"C:\Users\Szymon\Desktop\ds.pdf", FileMode.OpenOrCreate))
                {
                    int len;
                    while((len = reader.Read(buffer,0, buffer.Length)) > 0)
                    {
                        file.Write(buffer, 0, len);
                    }
                }
            }
            response.Close();

            return true;
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
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string result;
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                result = WebUtility.HtmlDecode(reader.ReadToEnd());
                reader.Close();
            }
            response.Close();
            return result;
        }
    }
}
