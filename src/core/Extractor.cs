using DotEnv = DotEnvFile.DotEnvFile;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;
using System;
using YuriDb.TMO;
using MySql.Data.MySqlClient;

namespace YuriDb
{
    public class Extractor
    {
        public static void Main(string[] args)
        {
            DotEnvSetup();
            YuriDb db = InstanciarDB();
	        TmoClient tc = new TmoClient();
	        
	        using(db.Connection)
	        using(tc.Client)
	        {
                db.CreateConnection();
                db.OpenConnection();
                Console.WriteLine("Inicializando...");
                db.CrearBaseDeDatos();
                db.CrearTablas();
                Console.WriteLine("¡Hecho!");

		        var pagina = tc.GetPagina(TmoClient.MangasYuri, 1, 1);
		        long nmangas = db.GetCantidadMangas();
		        
		        if (nmangas > 0) {
		            if ((Int32) pagina.Data["total"] == nmangas) {
		                Console.WriteLine("La base de datos está actualizada");
		                tc.Dispose();
		                db.Close();
		                db.Dispose();
		                return;
		            }
		            MangaYuri ultimo = db.GetUltimaAgregacion();
		            Console.WriteLine("Se debe actualizar, última agregación (TmoID = {0})", ultimo.TmoId);
		            int  busqueda = tc.EncontrarManga(ultimo);
                    if (busqueda > 0) {
                        Console.WriteLine("Se ha encontrado un intervalo favorable para TmoId = {0}", ultimo.TmoId);
                        pagina = tc.GetPagina(TmoClient.MangasYuri, (uint) busqueda / 60 + 1, 60);
                        AgregarPagina(pagina, db, tc, (int) busqueda % 60);
                    } else {
                        Console.WriteLine("Hubo un error interno, imposible resumir");
                        tc.Dispose();
                        db.Close();
                        db.Dispose();
                        Environment.Exit(-1);
                    }
                }  else {
		            pagina = tc.GetPagina(TmoClient.MangasYuri, 1, 60);
                    AgregarPagina(pagina, db, tc);
                }
                while (pagina.HasNext()) {
	                pagina = tc.GetPagina(pagina.BaseUri, pagina.NextPage(), 60);
	                AgregarPagina(pagina, db, tc);
	            }
		        tc.Dispose();
		        db.Close();
		        db.Dispose();
	        }
        }

        private static void AgregarPagina(TmoPage page, YuriDb db, TmoClient tmoClient)
        {
            AgregarPagina(page, db, tmoClient, 0);
        }

        private static void AgregarPagina(TmoPage page, YuriDb db, TmoClient tmoClient, int index)
        {
            JArray data = (JArray) page.Data["data"];
            for (int i = index; i < data.Count; i++) {

                if (page.Remaining <= 1) {
                    int s = 60;
                    do {
                        Console.WriteLine("Límite excedido, se continuará dentro de {0} segundos", s);
                        Thread.Sleep(1000);
                        s--;
                    } while (s > 0);
                }

                JObject obj = (JObject) data[i];
                JObject info = (JObject) obj["info"];
                MangaYuri manga =
                   new MangaYuri(
                     (UInt32) obj["id"],
                     DateTime.Parse((string) info["fechaCreacion"])
                   ) {
                       Nombre = (string) obj["nombre"],
                       Descripcion = (string) info["sinopsis"],
                       Imagen = new Uri("https://img1.tumangaonline.com/" + (string) obj["imageUrl"]),
                   };

                switch ((string) obj["tipo"]) {
                case "MANGA":
                    manga.Tipo = MangaTipo.Manga;
                    break;
                case "MANHUA":
                    manga.Tipo = MangaTipo.Manhua;
                    break;
                case "MANHWA":
                    manga.Tipo = MangaTipo.Manhwa;
                    break;
                case "NOVELA":
                    manga.Tipo = MangaTipo.Novela;
                    break;
                case "OTRO":
                    manga.Tipo = MangaTipo.Otro;
                    break;
                case "PROPIO":
                    manga.Tipo = MangaTipo.Propio;
                    break;
                }

                switch ((string) obj["estado"]) {
                case "Finalizado":
                    manga.Estado = MangaEstado.Finalizado;
                    break;
                case "Activo":
                    manga.Estado = MangaEstado.EnEmision;
                    break;
                }

                TmoPage capi = tmoClient.GetPagina(TmoClient.UriManga(manga.TmoId), 1, 1);
                manga.Capitulos = (UInt32) capi.Data["total"];
                page.InheritRateLimit(capi);
                
                try {
                	db.AgregarManga(manga);
                } catch (MySqlException e) {
                    if (e.Number != (int) MySqlErrorCode.DuplicateKeyEntry) {
                        throw e;
                    } else {
                        Console.WriteLine("[Info] Salteándose Manga (TmoId = {0})", manga.TmoId);
                        continue;
                    }
                }

                Console.WriteLine("[Info] [{3}/{4}] Creación Manga (TmoId={0}, rateLimit={1}, remaining={2})",
                    manga.TmoId,
                    page.RateLimit,
                    page.Remaining,
                    (((Int32) page.Data["current_page"] - 1) * 
                     (Int32.Parse((string) page.Data["per_page"])) + 
                     (i + 1)),
                    page.Data["total"]
                );
            }
        }

        private static YuriDb InstanciarDB()
        {
            YuriDb ydb = new YuriDb {
                Host = ObtenerYValidarExistencia("YURIDB_HOST"),
                User = ObtenerYValidarExistencia("YURIDB_USER"),
                Password = ObtenerYValidarExistencia("YURIDB_PASSWORD"),
                Db = ObtenerYValidarExistencia("YURIDB_DB")
            };

            try {
                ydb.Port = Int32.Parse(ObtenerYValidarExistencia("YURIDB_PORT"));
            } catch (FormatException) {
                Console.WriteLine("[Error] La variable de entorno YURIDB_PORT contiene un puerto inválido");
                Environment.Exit(-1);
            }
            return ydb;
        }

        private static void DotEnvSetup()
        {
	        var env = Environment.GetEnvironmentVariables();
	        if (!env.Contains("YURIDB_HOST")) {
	            try {
	                Dictionary<string, string> vars = DotEnv.LoadFile(".env");
	                DotEnv.InjectIntoEnvironment(vars);
	            }
	            catch (Exception) {
	                Console.WriteLine("[Error] El archivo .dotenv no existe o es inválido");
	                Environment.Exit(-1);
	            }
	        }
        }

        private static string ObtenerYValidarExistencia(string variable) {
            string result = Environment.GetEnvironmentVariable(variable);
            if (result == null) {
                Console.WriteLine("[Error] La variable de entorno {0} no se ha establecido", variable);
                Environment.Exit(-1);
            }
            return result;
        }
    }
}
