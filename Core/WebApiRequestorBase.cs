using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Sync.Cklass.Core
{
    public abstract class WebApiRequestorBase<T> where T : ICommObject
    {
        #region Internal Classes & Enums

        protected class WebApiRequest
        {
            public string SegmentUrl { get; set; }
            public HttpMethod Method { get; set; }
            public ICommObject Data { get; set; }
        }
        #endregion

        #region Memeber Variables
        private string _baseUrl;
        public string BaseUrl
        {
            get { return _baseUrl; }
            set { _baseUrl = value; }
        }
        public virtual bool RequestKeepAlive { get; set; }
        public virtual int RequestTimeout { get; set; }
        #endregion

        #region Constructors & Destructor
        protected WebApiRequestorBase(string baseUrl)
            : this()
        {
            _baseUrl = baseUrl.Trim('/');
            this.Intiliallize();
        }

        protected WebApiRequestorBase()
        {
            this.Intiliallize();
        }
        #endregion

        #region Virtual Methods
        protected virtual void Intiliallize()
        {
            this.RequestTimeout = 60000;
            this.RequestKeepAlive = true;
        }

        protected virtual T Request(WebApiRequest waRequest)
        {
            if (string.IsNullOrEmpty(_baseUrl))
            {
                throw new Exception("Missing base url for the request");
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_baseUrl + "/" + waRequest.SegmentUrl.Trim('/'));
            request.Method = Enum.GetName(typeof(HttpMethod), waRequest.Method);
            request.KeepAlive = this.RequestKeepAlive;
            request.Timeout = this.RequestTimeout;
            if (waRequest.Data != null && waRequest.Method != HttpMethod.GET)
            {
                request.ContentType = "application/json";
                var jsonData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(waRequest.Data));
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

        protected virtual async Task<T> RequestAsync(WebApiRequest waRequest)
        {
            HttpClient client = new HttpClient();
            string url = _baseUrl + "/" + waRequest.SegmentUrl.Trim('/');
            HttpContent content = null;
            if (waRequest.Method != HttpMethod.GET)
            {
                content = new StringContent(JsonConvert.SerializeObject(waRequest.Data), Encoding.UTF8,
                                            "application/json");
            }
            HttpResponseMessage response = null;
            switch (waRequest.Method)
            {
                case HttpMethod.GET:
                    response = await client.GetAsync(url);
                    break;
                case HttpMethod.POST:
                    response = await client.PostAsync(url, content);
                    break;
                case HttpMethod.PUT:
                    response = await client.PutAsync(url, content);
                    break;
                case HttpMethod.DELETE:
                    response = await client.DeleteAsync(url);
                    break;
                default:
                    throw new Exception("Http Method not supported"); 
            }
            var data = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(data);
        }

        #endregion

        #region Abstract Methods
        public abstract T Get(string id = null);

        public abstract Task<T> GetAsync(string id = null);

        public abstract T Post(T model);

        public abstract Task<T> PostAsync(T model);

        public abstract T Delete(string id);

        public abstract Task<T> DeleteAsync(string id);

        public abstract T Put(T model);

        public abstract Task<T> PutAsync(T model);
        #endregion
    }
}
