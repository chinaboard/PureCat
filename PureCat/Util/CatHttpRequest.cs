using System;
using System.IO;
using System.Net;

namespace PureCat.Util
{
    internal class CatHttpRequest
    {
        public static string GetRequest(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 5000;
                request.Method = "GET";
                request.Headers[HttpRequestHeader.IfNoneMatch] = Guid.NewGuid().ToString();
                using (var response = request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        var reader = new StreamReader(stream);
                        var text = reader.ReadToEnd();
                        return text;
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
