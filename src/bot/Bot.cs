using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System;
using YuriDataBase = YuriDb.Core.YuriDb;
using YuriDb.Bot.Modules;
using YuriDb.Core;


namespace YuriDb.Bot
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

               {
                    var sbAlts = new StringBuilder();
                    int i = 0;
                    foreach (MangaNomAlterno alterno in manga.NombresAlternos) {
                        if (i > 0) {
                            sbAlts.Append(", ");
                        }
                        sbAlts.Append(alterno.Nombre);
                        i++;
                    }
                    var alts = sbAlts.ToString();
                    var ebf = new EmbedFieldBuilder();
                    ebf.Name = "Nombres Alternos";
                    if (alts.Length == 0) {
                        ebf.Value = "Ninguno";
                    } else {
                        ebf.Value = alts;
                    }
                    eb.AddField(ebf);
                }

                {
                    var auts = from autor in manga.Staff where autor.Tipo == StaffTipo.Autor select autor;
                    var arts = from autor in manga.Staff where autor.Tipo == StaffTipo.Artista select autor;
                    var sbAuts = new StringBuilder();
                    var sbArts = new StringBuilder();
                    string val;

                    foreach (StaffManga autor in auts) {
                        sbAuts.Append(autor.Nombre);
                        sbAuts.Append('\n');
                    }

                    foreach(StaffManga artista in arts) {
                        sbArts.Append(artista.Nombre);
                        sbArts.Append('\n');
                    } 

                    val = sbAuts.ToString();
                    EmbedFieldBuilder efb = new EmbedFieldBuilder();
                    efb.Name = "Autor";
                    if (val.Length == 0) {
                        efb.Value = "Desconocido";
                    } else {
                        efb.Value = val;
                    }
                    efb.IsInline = true;
                    eb.AddField(efb);

                    val = sbAuts.ToString();
                    efb = new EmbedFieldBuilder();
                    efb.Name = "Artista";
                    if (val.Length == 0) {
                        efb.Value = "Desconocido";
                    } else {
                        efb.Value = val;
                    }
                    efb.IsInline = true;
                    eb.AddField(efb);
                }

                {
                    var efb = new EmbedFieldBuilder();
                    efb.IsInline = true;
                    efb.Name = "Revista";
                    if (manga.Revista != null) {
                        efb.Value = manga.Revista.Nombre;
                    } else {
                        efb.Value = "Desconocido";
                    }
                    eb.AddField(efb);

                    efb = new EmbedFieldBuilder();
                    efb.IsInline = true;
                    efb.Name = "Periodicidad";
                    if (manga.Revista != null && manga.Revista.Periodicidad != null) {
                        efb.Value = string.Format("{0:%d} días", manga.Revista.Periodicidad.Value);
                    } else {
                        efb.Value = "Desconocido";
                    }
                    eb.AddField(efb);
                }

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


    public class Bot
    {
        private static object _lock = new object();
        private static Bot _instancia;
        private static Bot Instancia 
        {
            get
            {
                lock (_lock) {
                    if (_instancia == null) {
                        _instancia = new Bot();
                    }
                    return _instancia;
                }
            }
        }

        private string _prefix;
        public string Prefix 
        {
            get
            {
                lock (_prefix) {
                    return _prefix;
                }
            }
            set
            {
                lock(_prefix) {
                    _prefix = value;
                }
            }
        }
        private DiscordSocketClient _client;
        private YuriDataBase _db;
        private CommandService _commands;
        private IServiceProvider _services;

        private Bot()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig {
                LogLevel = LogSeverity.Info
            });
            
            _db = YuriDataBase.Instancia;
            _db.CreateConnection();
            _db.OpenConnection();
            _db.CrearBaseDeDatos();
            _db.CrearTablas();
            _prefix = YuriDataBase.ValidarExistenciaEnv("YURIDB_BOT_PREFIX");            
            _client.Log += Log;
            _client.MessageReceived += MessageReceived;

            _commands = new CommandService();            

            var collection = new ServiceCollection();
            collection.AddSingleton(_db);
            collection.AddSingleton(_client);
            collection.AddSingleton(_commands);
            collection.AddSingleton(this);
            var actualizador = Actualizador.Instancia;
            collection.AddSingleton(Actualizador.Instancia);
            _services = collection.BuildServiceProvider();

            _commands.AddModulesAsync(Assembly.GetEntryAssembly())
              .GetAwaiter()
              .GetResult();
        }

        public static void Main(string[] args)
        {
        	MainAsync(args)
              .GetAwaiter()
              .GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            var bot = Bot.Instancia;
            await bot._client.LoginAsync(
                TokenType.Bot, 
                YuriDataBase.ValidarExistenciaEnv("YURIDB_BOT_TOKEN")
            );
            await bot._client.StartAsync();
            await Task.Delay(-1);
        }

        private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            
            int pos = -1;
            if (!MessageExtensions.HasStringPrefix(message, Prefix, ref pos)) {
                return;
            }
            var context = new CommandContext(_client, message);
            var result = await _commands.ExecuteAsync(context, pos, _services);
            if (!result.IsSuccess) {
                if (result.Error != CommandError.UnknownCommand) {
                    Console.WriteLine(result.ErrorReason);
                }
            }
        }

        private Task MangaAdded(MangaYuri manga)
        {
            EmbedBuilder eb = YuriEmbed.FromManga(manga);
            eb.ThumbnailUrl = eb.ImageUrl;
            eb.ImageUrl = null;
            Embed emb = eb.Build();
            foreach (SocketGuild guild in _client.Guilds) {
                guild.DefaultChannel.SendMessageAsync("**Nuevo manga**", false, emb);
            }
            return Task.CompletedTask;
        }
    }
}
