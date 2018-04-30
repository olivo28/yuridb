using Discord.Commands;
using Discord;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using YuriDb.Core;

namespace YuriDb.Bot.Modules 
{
    [Group("task")]
    [Alias("tarea")]
    [Name("Tareas")]
    [Summary("Tareas que corren en segundo plano")]
    public class RunModule: ModuleBase
    {
        [Group("actualizar")]
        [Alias("update")]
        [Name("Actualizador")]
        public class UpdateModule: ModuleBase 
        {
            public Actualizador Actualizador { get; set; }

            [Command("parar")]
            [Alias("stop")]
            public async Task Stop()
            {
                var efb = new EmbedBuilder() {
                    Title = "Actualizador",
                };
                if (Actualizador.IsUpdating) {
                        await Actualizador.StopAsync();
                        efb.Color = new Color((uint) EmbedColors.Info);
                        await ReplyAsync("", false, efb.WithDescription("Se ha interrumpido la actualización con éxtio").Build());
                        return;
                }
                efb.Color = new Color((uint) EmbedColors.Warning);
                await ReplyAsync("", false, efb.WithDescription("No hay ninguna actualización en curso").Build());
            }

            [Command("run")]
            [Alias("ejecutar")]
            private Task Run()
            {
                return Task.Run((Action) _run);    
            }

            private async void  _run()
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
}