using System;
using System.Net;

namespace PodBoy.Extension
{
    public static class UriExtensions
    {
        public static bool IsReachable(this Uri uri)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Timeout = 15000;
            request.Method = "HEAD";
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException)
            {
                return false;
            }
        }
    }
}