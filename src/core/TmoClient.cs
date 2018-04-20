using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace YuriDb.TMO
{
    public class TmoPage
    {

        public Uri BaseUri { get; private set; } = null;
        public JObject Data { get; private set; } = null;
        public uint RateLimit { get; private set; } = 0;
        public uint Remaining { get; private set; } = 0;

        public TmoPage(Uri baseUri, JObject data, uint rateLimit, uint remaining)
        {
            if (data == null || baseUri == null) { 
                throw new ArgumentNullException();
            }
            BaseUri = baseUri;
            Data = data;
            RateLimit = rateLimit;
            Remaining = remaining;
        }

        public bool HasNext()
        {
            return (UInt32) Data["last_page"] > (UInt32) Data["current_page"];
        }

        public uint NextPage()
        {
            return (UInt32) Data["current_page"] + 1;
        }

        public void InheritRateLimit(TmoPage page)
        {
            RateLimit = page.RateLimit;
            Remaining = page.Remaining;
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
            Client.Dispose();
        }

        public TmoPage GetPagina(Uri baseUri, uint page, uint itemsPerPage)
        {
            UriBuilder ub = new UriBuilder(baseUri);
            ub.Query = ub.Query.Substring(1) + $"&page={page}&itemsPerPage={itemsPerPage}";
            HttpResponseMessage response = Client.GetAsync(ub.Uri).GetAwaiter().GetResult();
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
            JObject data;
            try {
                data = JObject.Parse(content);
            } catch (JsonReaderException e) {
                throw new Exception("Salida extraña del servidor, contenido: \n" + content, e);
            }

            TmoPage result = new TmoPage(
                baseUri,
                data,
                UInt32.Parse(rateLimit),
                UInt32.Parse(remaining)
            );
            response.Dispose();
            return result;
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
                if (cache.Remaining == 0) {
                    Thread.Sleep(60 * 1000);
                }
                Console.WriteLine("Búscando en intervalo [{0}, {1}], actual = {2}", inf, sup, p);
                cache = GetPagina(MangasYuri, p + 1, 1);
                JObject obj = (JObject) (((JArray) cache.Data["data"])[0]);
                JObject info = (JObject) obj["info"];
                DateTime date = DateTime.Parse((string) info["fechaCreacion"]);

                if (manga.TmoCreacion.CompareTo(date) > 0) {
                    inf = p;
                } else if (manga.TmoCreacion.CompareTo(date) < 0) {
                    sup = p;
                } else {
                    return (int) p;
                }
                n++;
            }
            Console.WriteLine("Loop infinito detectado");
            return -1;
        }

        public static readonly Uri TmoUri = new Uri("https://www.tumangaonline.com");
        public static readonly Uri MangasYuri = new Uri("https://www.tumangaonline.com/api/v1/mangas?generos=[17]&sortDir=asc&sortedBy=fechaCreacion");

        public static Uri UriManga(uint tmoId)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("https://www.tumangaonline.com/api/v1/mangas/");
            sb.Append(tmoId);
            sb.Append("/capitulos?tomo=-1");
            return new Uri(sb.ToString());
        }

       
    }
}
