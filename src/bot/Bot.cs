using Discord.Commands;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Threading.Tasks;
using System;
using YuriDataBase = YuriDb.Core.YuriDb;
using YuriDb.Bot.Modules;
using YuriDb.Core;

namespace YuriDb.Bot
{
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
