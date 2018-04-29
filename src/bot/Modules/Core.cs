using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System; // eliminar luego
using YuriDataBase = YuriDb.Core.YuriDb;
using YuriDb.Bot;
using YuriDb.Core;

namespace YuriDb.Bot.Modules 
{
    public enum EmbedColors : uint 
    {
        Info = 0x4267b2,
        Warning = 0xff7f27,
        Error = 0xf16262
    }

    public class YuriEmbed 
    {
        public static EmbedBuilder FromManga(MangaYuri manga)
        {
                EmbedBuilder eb = new EmbedBuilder();
                eb.Title = manga.Nombre;
                eb.Description = manga.Descripcion;
                eb.ThumbnailUrl = manga.Imagen.ToString();
                eb.Color = new Color((uint) EmbedColors.Info);
                eb.Url = manga.GenerarLink().ToString();
                eb.AddField(new EmbedFieldBuilder()
                    .WithIsInline(true)
                    .WithName("Id")
                    .WithValue(manga.Id)
                );
                eb.AddField(new EmbedFieldBuilder()
                    .WithIsInline(true)
                    .WithName("TmoId")
                    .WithValue(manga.TmoId)
                );
                eb.AddField(new EmbedFieldBuilder()
                    .WithName("Tipo")
                    .WithIsInline(true)
                    .WithValue(manga.Tipo)
                );
                eb.AddField(new EmbedFieldBuilder()
                    .WithIsInline(true)
                    .WithName("Estado")
                    .WithValue(manga.Estado)
                );
                eb.AddField(new EmbedFieldBuilder()
                    .WithName("Capítulos (se incluyen especiales)")
                    .WithValue($"{manga.Capitulos}")
                );
                return eb;          
        }

        public static EmbedBuilder FromUpdateLog(List<MangaYuri> mangas, 
                                                 string status, 
                                                 uint i, 
                                                 uint r)
        {
            uint j = i;
            StringBuilder sb = new StringBuilder();
            sb.Append("```\n");
            foreach (MangaYuri manga in mangas) {
                sb.Append("[Info] ");
                sb.Append($"Nuevo elemento (TmoId={manga.TmoId}) [{j - mangas.Count + 2}/{i + 1 + r}]");
                sb.Append('\n');
                j++;
            }
            if (status != null) {
                sb.Append($"[Info] {status}");
            }
            sb.Append("```");
            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = "Actualización";
            eb.Description = sb.ToString();
            eb.Color = new Color((uint) EmbedColors.Info);
            return eb;
        }
    }

    [Name("Core")]
    public class CoreModule : ModuleBase
    {
        [Name("Ayuda")]
        public class HelpModule : ModuleBase{ 
            public CommandService Commands { get; set; }
            public Bot Bot { get; set; }

            [Command("modulos")]
            [Alias("modules")]
            [Summary("Muestra una lista de los módulos instalados")]
            public Task ListModules()
            {
                return Task.Run(() => {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.Title = "Lista de módulos instalados";
                    eb.Color = new Color((uint) EmbedColors.Info);
                    StringBuilder sb = new StringBuilder();
                    var search = 
                        from command in Commands.Modules
                        where (!command.IsSubmodule)
                        select command;
                    foreach (ModuleInfo module in search) {
                        sb.Append("• ");
                        sb.Append($"**{module.Name}**");
                        sb.Append('\n');
                    }
                    sb.Append('\n');
                    sb.Append($"Para ver los comandos de un módulo escribe `{Bot.Prefix}comandos <módulo>`");
                    eb.Description = sb.ToString();
                    ReplyAsync("", false, eb.Build());
                });
            } 

            [Command("comandos")]
            [Alias("cmds")]
            public Task ListCommands([Name("modulo"), Summary("Nombre del módulo a listar"), Remainder] String modulo = "Core")
            {
                return Task.Run(() => {
                    var query = from mod in Commands.Modules
                                where (mod.Name.ToLower().Equals(modulo.ToLower()))
                                select mod;
                    
                    
                    ModuleInfo module = null;
                    foreach (ModuleInfo m in query) {
                        module =  m;
                        break;
                    }
                    
                    if (module == null) {
                        ReplyAsync("El módulo no existe");
                        return;
                    }
                    
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.Title = "Comandos";
                    eb.Color = new Color((uint) EmbedColors.Info);
                    foreach (ModuleInfo submodule in module.Submodules) {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("```css\n");
                        var max = submodule.Commands.Max(x => x.Name.Length);
                        foreach (CommandInfo cmd in submodule.Commands) {
                            sb.Append(cmd.Name);
                            for (int i = max; i > cmd.Name.Length; i--) {
                                sb.Append(" ");
                            }
                            sb.Append("\t");
                            sb.Append(" [");
                            int j = 0;
                            foreach (string alias in (from a in cmd.Aliases where(a != cmd.Name) select a)) {
                                if (j > 0) {
                                    sb.Append(", ");
                                }
                                sb.Append(alias);
                                j++;
                            }
                            sb.Append("]\n");
                        }
                        sb.Append("```");
                        eb.AddField(new EmbedFieldBuilder()
                            .WithIsInline(true)
                            .WithName(submodule.Name)
                            .WithValue(sb.ToString())
                        );
                    }
                    ReplyAsync("", false, eb.Build());  

                });
            }

            [Command("ayuda")]
            [Alias("help")]
            [Summary("Muestar un mensaje de ayuda")]
            public Task Help()
            {
                return Task.Run(() => {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.Color = new Color((uint) EmbedColors.Info);
                    eb.Description = 
$@"Para ejecutar un comando escribe `{Bot.Prefix}<modulo> <comando>`
Donde:
`<modulo>`: Cualquiera de los módulos listados en `{Bot.Prefix}modulos`. 
Si se quiere usar un comando del módulo `Core` entonces éste debe dejarse vacío.
`<comando>`: Cualquier comando listado en `{Bot.Prefix}comandos <modulo>`.";
                    ReplyAsync("", false, eb.Build());
                });
            }
        }
    }

    [Group("db")]
    [Name("Db")]
    public class DbModule : ModuleBase
    {
        public YuriDataBase Db { get; set; }

        [Command("info"), Alias("stats")]
        [Summary("Devuelve las estadísticas de la BD.")]
        public Task Stats()
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
        public Task Ver([Name("id"), Summary("idel manga")] uint id)
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


        [Command("buscar")]
        [Alias("search")]
        [Summary("Busca un manga por su nombre y devuelve una lista de coincidencias")]
        public Task Buscar(
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

        private string MarkdownScape(string text) 
        {
            return text.Replace("*", "\\*")
                .Replace("[", "\\[")
                .Replace("]", "\\]")
                .Replace("(", "\\(")
                .Replace(")", "\\)");
        }
    }

    [Group("task")]
    [Name("Task")]
    [Summary("Commandos que corren en segundo plano")]
    public class RunModule : ModuleBase
    {
        public YuriDataBase Db { get; set; }
        public Actualizador Actualizador { get; set; }
        public Bot Bot {get; set;}
        
        [Command("parar")]
        [Alias("stop")]
        public async Task Stop([Name("comando"), Summary("El comando a parar"), Remainder] string comando)
        {
            if (comando == "actualizar" || comando == "update") {
                if (Actualizador.IsUpdating) {
                    await Actualizador.StopAsync();
                    await ReplyAsync($"Se ha finalizado el comando `{comando}` con éxito");
                    return;
                }
            }
            await ReplyAsync($"El comando `{comando}` no se está ejecutando");
        }

        [Command("actualizar")]
        [Alias("update")]
        private Task Update()
        {
            return Task.Run((Action) _update);    
        }

        private async void  _update()
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Color = new Color((uint) EmbedColors.Info);
            eb.Title = "Actualizador";
            if (!Actualizador.IsUpdating) {
                Task<uint> a = Actualizador.RunAsync();
                List<MangaYuri> log = new List<MangaYuri>();
                uint index = 0;
                uint remainder = 0;

                var message = await Context.Channel.SendMessageAsync("", false, YuriEmbed.FromUpdateLog(log, "Actualización en progreso", 0, 0).Build());

                Actualizador.AddMangaHandler handler = (m, i, r) => {
                    if (log.Count == 10) {
                        log.RemoveAt(0);
                    }
                    log.Add(m);
                    index = i;
                    remainder = r;
                    return Task.CompletedTask;
                };

                Actualizador.OnAdd += handler;

                int k = 0;
                char[] load = new char[] {'/','-', '|', '-', '\\', '|'};
                while (Actualizador.IsUpdating) {
                    string txt = null;
                    if (Actualizador.IsRateLimitExceeded) {
                        txt = $"Límite excedido [{load[k]}]";
                    } else if(log.Count == 0) {
                        txt = "Actualización en progreso";
                    }
                    await message.ModifyAsync((m) => {
                        m.Embed = YuriEmbed.FromUpdateLog(log, txt, index, remainder).Build();
                    });
                    k = (k + 1) % 5;
                    Thread.Sleep(1062);
                }

                Actualizador.OnAdd -= handler; 

                uint t = await a;
                if (t == 0) {
                    eb.Description = "La base de datos está actualizada";
                    await ReplyAsync("", false, eb.Build());
                } else {
                    eb.Description = $"Se han agregado {t} mangas";
                    await ReplyAsync("", false, eb.Build());
                }
                return;
            }
            eb.Description = "Ya hay una actualización en curso";
            eb.Color = new Color((uint) EmbedColors.Warning);
            await ReplyAsync("", false, eb.Build());
        }
    }
}