using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace YuriDb.Core
{
    public class TmoPage
    {
    	public TmoPage	Parent		{ get; private set; }
        public Uri 		BaseUri 	{ get; private set; }
        public JObject	Data 		{ get; private set; }
        public uint 	RateLimit 	{ get; private set; }
        public uint 	Remaining 	{ get; private set; }
        public DateTime TimeStamp 	{ get; private set; }

        public TmoPage(Uri baseUri, JObject data, uint rateLimit, 
        			   uint remaining, TmoPage parent)
        {
            if (data == null || baseUri == null) { 
                throw new ArgumentNullException();
            }
            BaseUri = baseUri;
            Data = data;
            RateLimit = rateLimit;
            Remaining = remaining;
            if (parent != null) {
            	parent.InheritRateLimit(this);
            	Parent = parent;
            }
            TimeStamp = DateTime.Now;
        }

        public TmoPage(Uri baseUri, JObject data, uint rateLimit, uint remaining) 
        	: this(baseUri, data, rateLimit, remaining, null)
        {

        }

        public bool HasNext()
        {
            return (UInt32) Data["last_page"] > (UInt32) Data["current_page"];
        }

        public uint NextPage()
        {
            return (UInt32) Data["current_page"] + 1;
        }

        public uint Count()
        {
            return (UInt32) Data["total"];
        }

        public void InheritRateLimit(TmoPage page)
        {
            RateLimit = page.RateLimit;
            Remaining = page.Remaining;
        }

        private TmoPage TopParent()
        {   
            TmoPage topParent = this;
            while (topParent.Parent != null) {
                topParent = topParent.Parent;
            }
            return topParent;            
        }

        public TimeSpan WaitTime() 
        {
            TimeSpan wait = TimeSpan.FromSeconds(105) - (DateTime.Now - TopParent().TimeStamp);
        	if (wait.Ticks >= 0) {
                return wait;
            } else {
                return TimeSpan.FromMilliseconds(0);
            }
        }

        public void Wait()
        {
            TimeSpan waitTime = WaitTime();
            if (waitTime.Ticks > 0) {
                Thread.Sleep(waitTime);
            }
            TmoPage parent = TopParent();
            parent.TimeStamp = DateTime.Now;
            parent.Remaining = RateLimit;
            Remaining = RateLimit;
        }
    }

    public class TmoClient
    {
        public HttpClient Client { get; private set; }

        public TmoClient()
        {
            Client = new HttpClient();
            var headers = Client.DefaultRequestHeaders;
            headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            headers.Connection.Add("keep-alive");
            headers.Add("Cache-mode", "no-cache");
            headers.Add("X-Requested-With", "XMLHttpRequest");
            headers.Referrer = TmoUri;
            headers.Host = "www.tumangaonline.com";
        }

        public void Dispose()
        {
            lock(Client) {
                Client.Dispose();
            }
        }

        public TmoPage GetPagina(Uri baseUri, uint page, uint itemsPerPage, 
        						 TmoPage parent)
        {
            UriBuilder ub = new UriBuilder(baseUri);
            ub.Query = ub.Query.Substring(1) + $"&page={page}&itemsPerPage={itemsPerPage}";

            HttpResponseMessage response;
            lock(Client) {
                response = Client.GetAsync(ub.Uri).GetAwaiter().GetResult();
            }
            string rateLimit = null;
            string remaining = null;
            {
                IEnumerator<string> i = response.Headers.GetValues("x-ratelimit-limit").GetEnumerator();
                i.MoveNext();
                rateLimit = i.Current;

                i = response.Headers.GetValues("x-ratelimit-remaining").GetEnumerator();
                i.MoveNext();
                remaining = i.Current;
            }

            string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            response.Dispose();

            JObject data;
            try {
                data = JObject.Parse(content);
            } catch (JsonReaderException) {
                throw new Exception($"Salida extraña del servidor. rateLimit={rateLimit}, remaining={remaining}, contenido={content}");
            }

            TmoPage result = new TmoPage(
                baseUri,
                data,
                UInt32.Parse(rateLimit),
                UInt32.Parse(remaining),
                parent
            );
            return result;
        }

        public TmoPage GetPagina(Uri baseUri, uint page, uint itemsPerPage)
        {
        	return GetPagina(baseUri, page, itemsPerPage, null);
        }


        public TmoPage GetPagina(Uri baseUri, uint page)
        {
            return GetPagina(baseUri, page, 25);
        }

        public int EncontrarManga(MangaYuri manga)
        {
            TmoPage cache = GetPagina(MangasYuri, 1, 1);

            uint inf = 0;
            uint sup = (UInt32) cache.Data["total"] - 1;

            int n = 0;
            int max = (int) Math.Ceiling((decimal) (Math.Log((double) sup) / Math.Log(2d)));

            while (n < max) { 
                uint p = (inf + sup) / 2;
                if (cache.RateLimit == 0) {
                    cache.Wait();
                }
                cache = GetPagina(MangasYuri, p + 1, 1);
                JObject obj = (JObject) (((JArray) cache.Data["data"])[0]);
                JObject info = (JObject) obj["info"];
                DateTime date = DateTime.Parse((string) info["fechaCreacion"]);

                if (manga.TmoCreacion.Value.CompareTo(date) > 0) {
                    inf = p;
                } else if (manga.TmoCreacion.Value.CompareTo(date) < 0) {
                    sup = p;
                } else {
                    return (int) p;
                }
                n++;
            }
            return -1;
        }

        public static readonly Uri TmoUri = new Uri("https://www.tumangaonline.com");
        public static readonly Uri MangasYuri = new Uri("https://www.tumangaonline.com/api/v1/mangas?generos=[17]&sortDir=asc&sortedBy=fechaCreacion");

        public static Uri ToUriManga(uint tmoId)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("https://www.tumangaonline.com/api/v1/mangas/");
            sb.Append(tmoId);
            sb.Append("/capitulos?tomo=-1");
            return new Uri(sb.ToString());
        }

       
    }
}
