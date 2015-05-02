using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using HtmlAgilityPack;

namespace AllDataSheetFinder.SiteParser
{
    public static class SiteActions
    {
        private const string SiteAddress = "http://www.alldatasheet.com/view.jsp";
        private const string SearchParameter = "Searchword";

        public static List<PartInfo> Search(string value)//, IProgress<int> progress)
        {
            WebClient client = new WebClient();
            client.QueryString.Add(SearchParameter, value);
            string result = WebUtility.HtmlDecode(client.DownloadString(SiteAddress));
            client.Dispose();

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
