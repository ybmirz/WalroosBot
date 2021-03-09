using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static AquacraftBot.Services.BotServices.BotServices;

namespace AquacraftBot.Services
{
    public sealed class HelpFormatter : BaseHelpFormatter
    {
        private readonly DiscordEmbedBuilder _output;
        private string description;
        public HelpFormatter(CommandContext ctx) : base(ctx)
        {
            _output = new DiscordEmbedBuilder()
                .WithColor(GlobalData.defaultColour)                
                .WithTimestamp(DateTime.Now)
                .WithFooter($"Requested by {ctx.User.Username}");
        }

        public override CommandHelpMessage Build()
        {
            _output.WithDescription(description);
            return new CommandHelpMessage(embed: _output);
        }

        //first method it will look to i!help <cmd> // might want to make a non embed help for this
        public override BaseHelpFormatter WithCommand(Command cmd)
        { 
            description = $"Command Description and Usage.\n[..] = Needed Argument(s)\n<..> = Optional Argument(s)";
            _output.WithAuthor("Command Help Page", null, GlobalData.logoURL);
            _output.ClearFields();

            StringBuilder sb = new StringBuilder();
            
                foreach (var overload in cmd.Overloads)
                {
                    sb.Append($"{GlobalData.prefixes[0]}{cmd.Name}"); // default just a name
                    if (overload.Arguments.Count >= 1)
                    {
                        sb.Append(" " + string.Join(" ", overload.Arguments.Select(xarg => xarg.IsOptional ? $"<{xarg.Name}>" : $"[{xarg.Name}]")));
                    }
                    sb.Append("\n");
                }
            _output.AddField(Formatter.Bold("# Usage"), Formatter.BlockCode(sb.ToString()));
            if (cmd.Aliases?.Any() ?? false) //needs changing
                _output.AddField("# Aliases", Formatter.BlockCode("<Aliases: "+ string.Join(" ", cmd.Aliases.Select(Formatter.InlineCode)) + ">", "xml"));
            _output.AddField(Formatter.Bold("# Description"), Formatter.BlockCode( "<Description: " + cmd.Description + ">", "xml"));

            return this;
        }

        //second method it looks to (used as main help formatter) i!help
        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            List<CmdGroup> module = new List<CmdGroup>();
            var enumerable = subcommands.ToList();
            if (enumerable.Any())
            {
                foreach (var cmd in enumerable)
                {
                    if (cmd.Parent is not CommandGroup)
                    {
                        description = $"Below is the list of commands!\nFor more info on specific commands and its usage, use `{GlobalData.prefixes[0]}help <command>`\nSomething went wrong? Contact dev through `{GlobalData.prefixes[0]}contact dev`";
                        _output.WithAuthor("Command List", null, GlobalData.logoURL);
                        _output.ClearFields();
                        // getting the cmd's group through their attributes
                        string groupName = string.Empty;
                        string groupEmoji = string.Empty;
                        foreach (var attr in cmd.CustomAttributes)
                        {
                            if (attr is GroupNameAttribute)
                            {
                                GroupNameAttribute a = (GroupNameAttribute)attr;
                                groupName = a.name;
                                groupEmoji = a.emoji;
                            }
                        }
                        bool exists = false;
                        foreach (var group in module)
                        {
                            if (group.GroupName == groupName)
                                exists = true;
                        }
                        // put outside to ensure it is one once per cmd
                        if (exists) //gets that a field already exists
                        {
                            module.Find(x => x.GroupName == groupName).cmdList.Add(cmd); //finds the groupname by name, and then adds the cmd
                        }
                        else // else it makes a new group and adds in the main module (filled with groups)
                        {
                            CmdGroup group = new CmdGroup(groupName, groupEmoji);
                            group.cmdList.Add(cmd);
                            module.Add(group);
                        }
                        //where we output all the groups
                        foreach (var group in module)
                        {
                            if(group.cmdList.Exists(cmd => cmd.Name.ToLower() != "help"))
                                _output.AddField(group.groupEmoji + " " + Formatter.Bold(group.GroupName), string.Join(" ", group.cmdList.Select(c => Formatter.InlineCode(c.Name))));
                        }
                    }
                    else
                    {
                        description = $"Command Description and Usage.\n[..] = Needed Argument(s)\n<..> = Optional Argument(s)";
                        _output.WithAuthor("Command Help Page", null, GlobalData.logoURL);
                        _output.ClearFields();
                        StringBuilder sb = new StringBuilder();
                        foreach (var overload in cmd.Overloads)
                        {
                            sb.Append($"{GlobalData.prefixes[0]}{cmd.Parent.Name} \n" + string.Join("|", cmd.Parent.Children.Select(c=> c.Name)) + "\n"); // default just a name
                        }
                        _output.AddField(Formatter.Bold("# Usage"), Formatter.BlockCode(sb.ToString()));
                        if (cmd.Aliases?.Any() ?? false) //needs changing
                            _output.AddField("# Aliases", Formatter.BlockCode("<Aliases: " + string.Join(" ", cmd.Aliases.Select(Formatter.InlineCode)) + ">", "xml"));
                        _output.AddField(Formatter.Bold("# Description"), Formatter.BlockCode("<Description: " + cmd.Description + ">", "xml"));
                    }
                }
            }
            return this;
        }
    }
}
