using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AllDataSheetFinder
{
    public static class Requests
    {
        public static HttpWebRequest CreateDefaultRequest(string url)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.UserAgent = "AllDataSheetFinder";
            request.Method = "GET";
            return request;
        }
        public static string ReadResponseString(HttpWebRequest request)
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
