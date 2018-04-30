using Discord.Commands;
using Discord;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using YuriDataBase = YuriDb.Core.YuriDb;
using YuriDb.Core;
using System.Text;

namespace YuriDb.Bot.Modules 
{
    [Name("Base")]
    [Summary("Comandos básicos del Bot")]
    public class CoreModule : ModuleBase
    {
        public CommandService Commands { get; set; }
        public Bot Bot { get; set; }

        [Command("modulos")]
        [Alias("modules")]
        [Summary("Muestra una lista de los módulos instalados")]
        public Task ListModules()
        {
            return Task.Run(() => {
                EmbedBuilder eb = new EmbedBuilder {
                    Title = "Lista de módulos instalados",
                    Color = new Color((uint) EmbedColors.Info)
                };
                foreach (var module in (from module in Commands.Modules 
                          where (!module.IsSubmodule)
                          select module)) {
                            eb.AddField(CreateFieldFor(module));
                }
                ReplyAsync("", false, eb.Build());
            });
        } 

        private static EmbedFieldBuilder CreateFieldFor(ModuleInfo module)
        {
            var efb = new EmbedFieldBuilder();
            var sb = new StringBuilder();
            efb.IsInline = true;
            sb.Append(module.Name);
            sb.Append(" [");
            {
                int i = 0;
                foreach (var alias in module.Aliases) {
                    if (i++ != 0) {
                        sb.Append(", ");
                    }
                    sb.Append(alias);
                }
            }
            sb.Append(']');
            efb.Name = sb.ToString();
            sb = new StringBuilder();
            sb.Append(module.Summary ?? "Sin descripción");
            efb.Value = sb.ToString();
            return efb;
        }

        [Command("comandos")]
        [Alias("cmds")]
        public Task ListCommands([Name("modulo"), Summary("Nombre del módulo a listar"), Remainder] String modulo = "")
        {
            return Task.Run(() => {
                EmbedBuilder eb = new EmbedBuilder();
                eb.Color = new Color((uint) EmbedColors.Info);
                eb.Title = "Comandos";
                {
                    var modules = from module in Commands.Modules
                      where (!module.IsSubmodule && ContainAlias(module.Aliases, modulo))
                      select module;
                    var i = modules.GetEnumerator();
                    if (i.MoveNext()) {
                        eb.Title += " en el módulo: " + i.Current.Name;
                        foreach (var sub in i.Current.Submodules) {
                            eb.AddField(CreateCommandFieldFor(sub));
                        }
                        var j = i.Current.Commands.GetEnumerator();
                        if (j.MoveNext()) {
                            eb.AddField(CreateCommandFieldFor(i.Current)
                                .WithName("Sin categoría")
                                .WithIsInline(false)
                            );
                        }
                    } else {
                        eb.Description = "Módulo no encontrado";
                        eb.Color = new Color((uint) EmbedColors.Warning);
                    }
                }
                ReplyAsync("", false, eb.Build());
            });
        }

        private static bool ContainAlias(IEnumerable<string> aliases, string alias)
        {
            var calias = from a in aliases 
               where (alias.Equals(a))
               select a;
            if (calias.GetEnumerator().MoveNext()) {
                return true; 
            }
            if (alias.Equals("") && !aliases.GetEnumerator().MoveNext()) {
                return true;
            }
            return false;
        }

        private static EmbedFieldBuilder CreateCommandFieldFor(ModuleInfo submodule)
        {
            var efb = new EmbedFieldBuilder().WithIsInline(true);
            var sb = new StringBuilder();
            sb.Append("```css\n");            var cmds = from cmd in submodule.Commands 
              select SimpleAliasName(Enumerable.ToArray<string>(cmd.Aliases)[0]);
            var max = cmds.Max((cmd) => cmd.Length);            foreach (var cmd in submodule.Commands) {
                var aliases = Enumerable.ToArray<string> (from g in 
                  (from sa in (from a in cmd.Aliases select SimpleAliasName(a)) group sa 
                   by SimpleAliasName(sa))
                  select g.Key
                );
                sb.Append(aliases[0]);
                for (int j = max; j > aliases[0].Length; j--) {
                    sb.Append(' ');
                }
                sb.Append(" [");
                for (int i = 1; i < aliases.Length; i++) {
                    sb.Append(aliases[i]);
                }
                sb.Append(']');
                sb.Append('\n');
            }
            sb.Append("```");
            efb.Value = sb.ToString();
            sb = new StringBuilder();
            sb.Append(submodule.Name);
            sb.Append(" [");
            { 
                int i = 0;
                foreach (var alias in (from g in (from a in submodule.Aliases
                         group a by SimpleAliasName(a)) select g.Key)) {
                           if (i++ != 0) {
                               sb.Append(", ");
                           }
                           sb.Append(alias);
                }
            }
            sb.Append(']'); 
            sb.Append(' ');
            efb.Name = sb.ToString();  
            return efb;
        }

        private static string SimpleAliasName(string fullname)
        {
            var sp = fullname.Split(' ');
            return sp[sp.Length - 1];
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
`<comando>`: Cualquier comando listado en `{Bot.Prefix}comandos <modulo>`.";
                ReplyAsync("", false, eb.Build());
            });
        }
    }

}