using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AllDataSheetFinder
{
    public struct NewVersionInfo
    {
        public Version Version;
        public string Link;
        public string Execute;
        public string Files;

        public async static Task<NewVersionInfo> RequestInfoAsync(string url)
        {
            NewVersionInfo info = new NewVersionInfo();

            HttpWebRequest request = Requests.CreateDefaultRequest(url);
            string result = await Requests.ReadResponseStringAsync(request);

            XElement rootElement = XElement.Parse(result);
            XElement versionElement = rootElement.Element("version");
            XElement downloadElement = rootElement.Element("download");
            XElement executeElement = rootElement.Element("execute");
            XElement filesElement = rootElement.Element("files");

            info.Version = Version.Parse(versionElement.Value);
            info.Link = downloadElement.Value;
            info.Execute = executeElement.Value;
            info.Files = filesElement.Value;

            return info;
        }
    }
}
