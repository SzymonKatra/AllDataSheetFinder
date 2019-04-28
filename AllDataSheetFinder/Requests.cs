using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AllDataSheetFinder
{
    public static class Requests
    {
        public static HttpWebRequest CreateDefaultRequest(string url)
        {
            HttpWebRequest request = WebRequest.CreateHttp(GetValidUri(url));
            request.UserAgent = Global.RequestsUserAgent;
            request.Method = WebRequestMethods.Http.Get;
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
        public async static Task<string> ReadResponseStringAsync(HttpWebRequest request)
        {
            string result;
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    result = WebUtility.HtmlDecode(reader.ReadToEnd());
                }
            }
            return result;
        }

        private static Uri GetValidUri(string url)
        {
            if (url.StartsWith("//")) url = url.Remove(0, 2);
            return new UriBuilder(url) { Scheme = "http" }.Uri;
        }
    }
}
