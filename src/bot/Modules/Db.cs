using Discord.Commands;
using Discord.WebSocket;
using Discord;
using YuriDataBase = YuriDb.Core.YuriDb;
using YuriDb.Core;
using YuriDb.Bot;
using System.Threading.Tasks;
using System.Threading;
using System.Text;

namespace YuriDb.Bot.Modules 
{
    [Group("db")]
    [Name("YuriDb")]
    [Summary("Comandos que consumen la base de datos")]
    public class DbModule : ModuleBase
    {
        public YuriDataBase Db { get; set; }

        [Name("Búsqueda")]
        [Group("buscar")]
        [Alias("search")]
        public class BusquedaModule : ModuleBase
        {
            public YuriDataBase Db { get; set; }
            
            [Command("nombre")]
            [Alias("name")]
            [Summary("Busca un manga por su nombre y devuelve una lista de coincidencias")]
            public Task SearchByName(
                [Name("nombre"), Summary("Nombre del manga a buscar"), Remainder] string nombre)
            {
                return Task.Run(() => {
                    MangaYuri[] mangas = Db.GetMangas(nombre);
                    if (mangas.Length == 0) {
                        ReplyAsync("Ninguna coincidencia");
                        return;
                    }
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.Color = new Color((uint) EmbedColors.Info);
                    StringBuilder sb = new StringBuilder();
                    int i = 1;
                    foreach (MangaYuri manga in mangas) {
                        sb.Append("**");
                        sb.Append('[');
                        sb.Append(i);
                        sb.Append(". ");
                        sb.Append(MarkdownScape(manga.Nombre));
                        sb.Append(' ');
                        sb.Append("\\[");
                        sb.Append(manga.Id.Value);
                        sb.Append("\\] ");
                        sb.Append(']');
                        sb.Append('(');
                        sb.Append(MarkdownScape(manga.GenerarLink().ToString()));
                        sb.Append(')');
                        sb.Append("**");
                        sb.Append('\n');
                        sb.Append($"**{manga.Tipo.ToString()} {manga.Estado.ToString()}**");
                        sb.Append('\n');
                        sb.Append($"Capítulos: {manga.Capitulos}");
                        sb.Append('\n');
                        i++;
                    }
                    sb.Append("\n**Nota**: se incluyen los capítulos especiales en el número de capítulos");
                    eb.Title = "Búsqueda para " + nombre;
                    eb.Description = sb.ToString();
                    ReplyAsync("", false, eb.Build()); 
                });         
            }

            private static string MarkdownScape(string text) 
            {
                return text.Replace("*", "\\*")
                    .Replace("[", "\\[")
                    .Replace("]", "\\]")
                    .Replace("(", "\\(")
                    .Replace(")", "\\)");
            }
        }


        [Command("info")]
        [Alias("stats")]
        [Summary("Devuelve las estadísticas de la BD.")]
        public Task Mostrar()
        {
            return Task.Run(() => {
                EmbedBuilder eb = new EmbedBuilder();       
                eb.AddField(new EmbedFieldBuilder()
                    .WithName("Mangas")
                    .WithIsInline(true)
                    .WithValue(Db.GetCantidadMangas(MangaEstado.Todos))
                );
                eb.AddField(new EmbedFieldBuilder()
                    .WithName("Finalizados")
                    .WithIsInline(true)
                    .WithValue(Db.GetCantidadMangas(MangaEstado.Finalizado))
                );
                eb.AddField(new EmbedFieldBuilder()
                    .WithName("Activos")
                    .WithIsInline(true)
                    .WithValue(Db.GetCantidadMangas(MangaEstado.Activo))
                );
                eb.AddField(new EmbedFieldBuilder()
                    .WithName("Abandonados")
                    .WithIsInline(true)
                    .WithValue(Db.GetCantidadMangas(MangaEstado.Abandonado))
                );
                MangaYuri ultimo = Db.GetUltimaAgregacion();
                if (ultimo != null) {
                    eb.AddField(new EmbedFieldBuilder()
                        .WithName("Último manga agregado")
                        .WithValue(ultimo.Nombre)
                    );
                    eb.ThumbnailUrl = ultimo.Imagen.ToString();
                }
                eb.Title = "Estadísticas";
                eb.Color = new Color((uint) EmbedColors.Info);
                ReplyAsync("", false, eb.Build());
            });
        }


        [Command("random")]
        [Summary("Obtiene un manga al azar.")]
        public Task Random()
        {
            return Task.Run(() => {
                MangaYuri manga = Db.GetMangaRandom();
                ReplyAsync("", false, YuriEmbed.FromManga(manga).Build());
            });
        }

        [Command("ver")]
        [Alias("show")]
        [Summary("Busca un manga por su nombre y muestra la primer coincidencia.")]
        public Task Show([Name("nombre"), Summary("nombre del manga a buscar"), Remainder] string nombre)
        {
            return Task.Run(() => {
                MangaYuri manga = Db.GetManga(nombre);
                if (manga == null) {
                    ReplyAsync("Manga no encontrado");
                    return;
                }
                ReplyAsync("", false, YuriEmbed.FromManga(manga).Build());
            });
        }

        [Command("ver")]
        [Alias("show")]
        [Summary("muestra la información de un manga por su id")]
        public Task Ver([Name("id"), Summary("id del manga")] uint id)
        {
            return Task.Run(() => {
                MangaYuri manga = Db.GetManga(id);
                if (manga == null) {
                    ReplyAsync("Id no encontrado");
                    return;
                }
                ReplyAsync("", false, YuriEmbed.FromManga(manga).Build());
            });
        }
    }
	
}