using AquacraftBot;
using AquacraftBot.Services;
using AquacraftBot.Services.BotServices;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IamagesDiscordBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var json = string.Empty;
            using (var fs = File.OpenRead("./Resources/config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = sr.ReadToEnd();

            var config = JsonConvert.DeserializeObject<BotConfiguration>(json);
            var bot = new Bot(config);
            bot.RunAsync().GetAwaiter().GetResult();
            //text file closing 
        }
    }
    public class Bot
    {
        private static string _BotName { set; get; }
        private string _token { set; get; }
        private List<string> _prefixes { set; get; } = new List<string>();
        private bool _EnableModeration { set; get; } = true;
        private bool _EnableSuggestions { set; get; } = false;
        private bool _EnableFunCommands { set; get; } = false;
        private bool _EnableModCommands { set; get; } = false;
        private bool _EnableUtilCommands { set; get; } = false;

        public Bot(BotConfiguration BotConfig)
        {
            _BotName = BotConfig.BotName;
            _token = BotConfig.token;
            foreach (string prefix in BotConfig.prefixes)
            {
                _prefixes.Add(prefix);
            }
            _EnableModeration = BotConfig.EnableModeration;
            _EnableSuggestions = BotConfig.EnableSuggestions;
            _EnableFunCommands = BotConfig.EnableFunCommands;
            _EnableModCommands = BotConfig.EnableModCommands;
            _EnableUtilCommands = BotConfig.EnableUtilCommands;
        }

        public readonly EventId BotEventId = new EventId(42, _BotName);
        public DiscordClient _Client { get; private set; }
        public CommandsNextExtension _Commands { get; private set; }
        public InteractivityExtension _Interactivity { get; private set; } // not yet set for now

        public async Task RunAsync()
        {

            var config = new DiscordConfiguration
            {
                Token = _token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug,
                LargeThreshold = 500,
                Intents = DiscordIntents.DirectMessages | DiscordIntents.Guilds | DiscordIntents.GuildMembers | DiscordIntents.GuildMessages | DiscordIntents.GuildMessageReactions | DiscordIntents.GuildBans | DiscordIntents.GuildInvites// only looks at DMs for events
            };

            _Client = new DiscordClient(config);
            // every client ready, guild available and client error
            _Client.Ready += Client_OnReady;
            _Client.GuildAvailable += Client_GuildConnected;
            _Client.ClientErrored += Client_ClientError;

            //might wanna add a interactivity here along with its config
            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = _prefixes,
                EnableDms = false, // for now
                EnableMentionPrefix = false,
                EnableDefaultHelp = true,
                DmHelp = false
            };

            _Commands = _Client.UseCommandsNext(commandsConfig);
            _Commands.CommandExecuted += Command_CommandExecuted;
            _Commands.CommandErrored += Command_CommandError;


            _Commands.RegisterCommands<Commands>();
            //_Commands.RegisterCommands<UtilCmds>();
            //_Commands.SetHelpFormatter<HelpFormatter>();


            //client connection to bot application on the discord api
            await _Client.ConnectAsync();
            //assigning global data values
            GlobalData.startTime = DateTime.Now;
            GlobalData.prefixes = _prefixes;

            await Task.Delay(-1);
        }

        //logging stuff onto the console line (for all of these might want to log into a text file if needed)
        private Task Client_OnReady(DiscordClient sender, ReadyEventArgs e)
        {
            sender.Logger.LogInformation(BotEventId, $"The {_BotName} is up and ready with the following prefixes: {string.Join(",", _prefixes) ??  "No prefix provided"}");
            return Task.CompletedTask;
        }
        private Task Client_GuildConnected(DiscordClient sender, GuildCreateEventArgs e)
        {
            sender.Logger.LogInformation(BotEventId, $"{_BotName} is now connected to \"{e.Guild.Name}\"({e.Guild.Id})");
            //read the prefixes json and then read (not needed right now, as only one main prefix at hand)
            return Task.CompletedTask;
        }
        private Task Client_ClientError(DiscordClient sender, ClientErrorEventArgs e)
        {
            sender.Logger.LogError(BotEventId, $"Oh no client error, [Type: {e.Exception.GetType()}] [{e.Exception.Message}]");
            return Task.CompletedTask;
        }
        private Task Command_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            sender.Client.Logger.LogInformation(BotEventId, message: $"{e.Context.User} has executed \"{e.Command.Name}\" cmd with message '{e.Context.Message.Content}' [{e.Context.Channel.Name} ({e.Context.Channel.Id})]"); //for now, not yet implement guilds in the log
            return Task.CompletedTask;
        }
        private async Task Command_CommandError(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            await ErrorHandlers.Process(e, BotEventId);
            //default logging to console below:
            sender.Client.Logger.LogWarning(BotEventId, $"{e.Command.Name} Command Error: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"} by [{e.Context.User}] in [{e.Context.Channel.Name} ({e.Context.Channel.Id})] "); //changes from time to time
        }
    }
}
