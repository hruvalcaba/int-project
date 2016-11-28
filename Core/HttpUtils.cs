using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Sync.Cklass.Core
{
    public enum HttpMethod
    {
        GET,
        POST,
        PUT,
        DELETE
    }

    public class HttpUtils
    {
        public static T Request<T>(string url, HttpMethod method, object Data, bool keepAlive = true, int TimeOut = 60000)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new Exception("Missing base url for the request");
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = Enum.GetName(typeof(HttpMethod), method);
            request.KeepAlive = keepAlive;
            request.Timeout = TimeOut;
            if (Data != null && method != HttpMethod.GET)
            {
                request.ContentType = "application/json";
                var jsonData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Data));
                request.ContentLength = jsonData.Length;
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(jsonData, 0, jsonData.Length);
                }
            }
            var response = (HttpWebResponse)request.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream());
            var jsonString = sr.ReadToEnd();
            sr.Close();
            sr.Dispose();
            return JsonConvert.DeserializeObject<T>(jsonString);
        }
    }
}
