﻿using System;
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
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 0; AllDataSheetFinder)";
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
    }
}
