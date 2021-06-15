using AquacraftBot.Commands.FunCmds;
using AquacraftBot.Commands.GiveawayCmds;
using AquacraftBot.Commands.ModerationCmds;
using AquacraftBot.Commands.SuggestionCmds;
using AquacraftBot.Commands.UtilCommands;
using AquacraftBot.Services;
using AquacraftBot.Services.BotServices;
using AquacraftBot.Services.SuggestionServices;
using AquacraftBot.Services.TickettingServices;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace AquacraftBot
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls("http://localhost:5002", "http://localhost:5003");
            });
    }
    public class Bot
    {
        //Things needed for general purposes of the bot
        private static string _BotName { set; get; }
        private string _token { set; get; }
        private List<string> _prefixes { set; get; } = new List<string>();

        //Things needed to connect to the discord API

        public readonly EventId BotEventId = new EventId(id: 42, name: _BotName);
        public DiscordClient _Client { get; private set; }
        public CommandsNextExtension _Commands { get; private set; }
        public InteractivityExtension _Interactivity { get; private set; } // not yet set for now
        public int _count { get; private set; }

        public Bot(BotConfiguration BotConfig, IServiceProvider services)
        {
            _BotName = BotConfig.BotName;
            _token = BotConfig.token;
            foreach (string prefix in BotConfig.prefixes)
            {
                _prefixes.Add(prefix);
            }
            GlobalData.logoURL = BotConfig.LogoURL;

            var config = new DiscordConfiguration
            {
                Token = _token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug,
                LargeThreshold = 500,           
                Intents = DiscordIntents.AllUnprivileged           
            };

            _Client = new DiscordClient(config);
            // every client ready, guild available and client error
            _Client.Ready += Client_OnReady;
            _Client.GuildAvailable += Client_GuildConnected;
            _Client.ClientErrored += Client_ClientError;

            //everytime a channel is made in the guild
            _Client.ChannelCreated += Client_ChannelCreated;
            _Client.MessageReactionAdded += _Client_MessageReactionAdded;
            _Client.MessageCreated += _Client_MessageCreated;
            _Client.ChannelDeleted += _Client_ChannelDeleted;
            _Client.MessageAcknowledged += _Client_MessageAcknowledged;

            //might wanna add a interactivity here along with its config
            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = _prefixes,
                EnableDms = true,
                EnableMentionPrefix = false,
                EnableDefaultHelp = true,
                DmHelp = false,
                Services = services
            };

            _Commands = _Client.UseCommandsNext(commandsConfig);
            _Commands.CommandExecuted += Command_CommandExecuted;
            _Commands.CommandErrored += Command_CommandError;

            if (BotConfig.EnableSuggestions)
            {
                _Commands.RegisterCommands<SuggestionCmds>();
                var json = string.Empty;
                using (var fs = File.OpenRead("./Resources/SuggestionConfig.json"))
                using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                    json = sr.ReadToEnd();
                SuggestionConfiguration suggestConfig = SuggestionConfiguration.GetConfig(json);
                SuggestionService.SuggestionChannel = suggestConfig.SuggestionChannel;
                SuggestionService.DecisionChannel = suggestConfig.DecisionChannel;
                SuggestionService.VotingThreshold = suggestConfig.Threshold;
                SuggestionService.DMonDecision = suggestConfig.DMonDecision;
            }
            if (BotConfig.EnableUtilCommands)
                _Commands.RegisterCommands<UtilCommands>();
            if (BotConfig.EnableModeration)
            {
                _Commands.RegisterCommands<ModerationCmds>();
                _Commands.RegisterCommands<AnnouncerCmd>();
            }
            if (BotConfig.EnableFunCommands)
            {
                _Commands.RegisterCommands<EightBallCmd>();
                _Commands.RegisterCommands<RPSCmd>();
            }
            if (BotConfig.EnableGiveaway)
            {
                _Commands.RegisterCommands<GiveawayCmd>();   
            }
            
            //_Commands.RegisterCommands<UtilCmds>();
            _Commands.SetHelpFormatter<HelpFormatter>();

            _Client.UseInteractivity(new InteractivityConfiguration()
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
                Timeout = TimeSpan.FromSeconds(30),
                PollBehaviour = PollBehaviour.KeepEmojis,                
            });

            //client connection to bot application on the discord api
            _Client.ConnectAsync();

            // Change the playing thing on bot's discord
            DiscordActivity playActivity = new DiscordActivity()
            {
                ActivityType = ActivityType.Playing,
                Name = "play.aquacraft.ca"
            };
            _Client.UpdateStatusAsync(playActivity);
            Timer statusTimer = new Timer(20000); // 20 seconds of interval between statuses
            statusTimer.Elapsed += StatusTimer_Elapsed;
            statusTimer.Start();

            // assigning global data values            
            GlobalData.startTime = DateTime.Now;
            GlobalData.prefixes = _prefixes;
            GlobalData.botName = _BotName;            
            // Adds in faq questions
            GlobalData.faqs.Add("ip");            
        }

        private Task _Client_MessageAcknowledged(DiscordClient sender, MessageAcknowledgeEventArgs msg)
        {
            if (msg.Channel is not DiscordDmChannel)
            {                
                if (GlobalData.faqs.Any(msg.Message.Content.Contains))
                {
                    var content = msg.Message.Content;                    
                    if ((content.Contains("what") || content.Contains("where")) && content.Contains("ip")) // Just add some more when adding more into the list 
                    {
                        // IP Question
                        msg.Channel.SendMessageAsync("Our ip is `play.aquacraft.ca`. Come join us!").ConfigureAwait(false);                        
                    }
                    // Add more below
                }

                if (msg.Channel.Name.Contains("bump")) // bump reminder things
                {
                    if (GlobalData.enableBumpReminder)
                    {
                        if (msg.Message.Author.Id == 302050872383242240)
                        {
                            sender.Logger.LogInformation("Bump found!");
                            var embeds = msg.Message.Embeds;
                            bool bumpDone = false;
                            foreach (var embed in embeds)
                            {
                                if (embed.Description.Contains("Bump done"))
                                    bumpDone = true;
                            }

                            if (bumpDone)
                            {
                                BumpReminder.BumpThanks(msg.Message.MentionedUsers[0], msg.Channel);
                                var Reminder = new Timer(TimeSpan.FromHours(2).TotalMilliseconds);
                                Reminder.Elapsed += (sender, e) => BumpReminder.RemindElapsed(sender, e, msg, Reminder);
                                Reminder.AutoReset = false;
                                Reminder.Start();
                            }
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }

        private Task _Client_ChannelDeleted(DiscordClient sender, ChannelDeleteEventArgs e)
        {
            try {
                var msg = GlobalData.ticketMsgs.Find(msg => msg.Channel.Id == e.Channel.Id);
                GlobalData.ticketMsgs.Remove(msg);
                sender.Logger.LogInformation($"Unreacted ticket message found in channel {e.Channel.Name} in {e.Guild.Name} ({e.Guild.Id})");
            }
            catch { sender.Logger.LogInformation($"Channel {e.Channel.Name} has been deleted in {e.Guild.Name} ({e.Guild.Id})"); }
            return Task.CompletedTask;
        }

        private void StatusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {            
            List<DiscordActivity> activites = new List<DiscordActivity>();            
            DiscordActivity listeningActivity = new DiscordActivity()
            {
                ActivityType = ActivityType.ListeningTo,
                Name = "walroos mating sounds"
            };
            activites.Add(listeningActivity);
            DiscordActivity watchingActivity = new DiscordActivity()
            {
                ActivityType = ActivityType.Watching,
                Name = $"users for {(DateTime.Now - GlobalData.startTime).TotalMinutes} minutes"
            };
            activites.Add(watchingActivity);
            DiscordActivity fireActivity = new DiscordActivity()
            {
                ActivityType = ActivityType.Watching,
                Name = "for w!firestarters"
            };
            activites.Add(fireActivity);
            DiscordActivity playActivity = new DiscordActivity()
            {
                ActivityType = ActivityType.Playing,
                Name = "play.aquacraft.ca"
            };
            activites.Add(playActivity);
            _Client.UpdateStatusAsync(activites[_count % 4]);
            _count++;
        }

        private Task _Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs msg)
        {
            if (msg.Channel is not DiscordDmChannel)
            {
                if (GlobalData.faqs.Any(msg.Message.Content.Contains))
                {                     
                    var content = msg.Message.Content;
                    if ((content.Contains("what") || content.Contains("where")) && (content.Contains("ip") || content.Contains(" ip")) || content.Contains("?")) // Just add some more when adding more into the list 
                    {
                        // IP Question
                        msg.Channel.SendMessageAsync("Our ip is `play.aquacraft.ca`. Come join us!").ConfigureAwait(false);
                    }
                }
            }
                return Task.CompletedTask;
        }

        private async Task _Client_MessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
        {            
            sender.Logger.LogInformation($"{e.User} has reacted with `{e.Emoji.GetDiscordName()}` in {e.Channel}[Guild: {e.Guild}]");

            if (GlobalData.ticketMsgs.Contains(e.Message)) // if the user reacts in the message 
            {
                Console.WriteLine("Ticket message reacted!");
                var msgIndex = GlobalData.ticketMsgs.IndexOf(e.Message);
                var msg = GlobalData.ticketMsgs[msgIndex];
                if (e.User == msg.MentionedUsers[0])
                {
                    var ticket = e.Channel;
                    DiscordEmoji chosen = e.Emoji;                    
                    await msg.DeleteAllReactionsAsync().ConfigureAwait(false);
                    if (chosen == null)
                    {
                        await msg.ModifyAsync("Request timed out/Not chosen. Please follow the form below, staff will be with you soon.", embed: null).ConfigureAwait(false);
                        await TicketterServices.GeneralSupportTicketAsync(ticket).ConfigureAwait(false);
                        return;
                    }
                    else
                        await msg.ModifyAsync("Thank you for complying. Please follow the form below and staff will be with you soon.", embed: null).ConfigureAwait(false);

                    switch (chosen.GetDiscordName())
                    {
                        case ":exclamation:":
                            await TicketterServices.PlayerReportTicketAsync(ticket).ConfigureAwait(false);
                            break;
                        case ":space_invader:":
                            await TicketterServices.BugReportTicketAsync(ticket).ConfigureAwait(false);
                            break;
                        case ":scroll:":
                            await TicketterServices.PunishmentAppealTicketAsync(ticket).ConfigureAwait(false);
                            break;
                        case ":tada:":
                            await TicketterServices.GiveawayRedeem(ticket).ConfigureAwait(false);
                            break;
                        case ":mailbox_with_mail:":
                            await TicketterServices.GeneralSupportTicketAsync(ticket).ConfigureAwait(false);
                            break;
                        case ":homes:":
                            await TicketterServices.BuildEntry(ticket).ConfigureAwait(false);
                            break;
                        default:
                            await TicketterServices.GeneralSupportTicketAsync(ticket).ConfigureAwait(false);
                            break;
                    }
                    GlobalData.ticketMsgs.Remove(msg); // Remove after it has been used.
                }
            }
        }

        private async Task Client_ChannelCreated(DiscordClient sender, ChannelCreateEventArgs e)
        {
            //needs a lot of editing for more than one server but for now it's fine
            if (e.Channel.Name.Contains("ticket")) // if the name of the channel created has "ticket" in the name
            {
                sender.Logger.LogInformation($"Ticket channel {e.Channel.Name} found!");
                if (GlobalData.enableTicketter)
                {
                    sender.Logger.LogInformation("Ticketter is enabled! Starting ticketter.");
                    await Task.Delay(2500);                    
                    var ticket = e.Channel;
                    var msg = await TicketterServices.SendTicketInitiationEmbed(sender, e.Guild, ticket);
                    GlobalData.ticketMsgs.Add(msg);
                }
            }            
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
            // Set up maybe 3 guild emotes to be used by the bot for now.
            GlobalData.AquaEmotes = e.Guild.Emojis;
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
            if (e.Command != null)
            {
                sender.Client.Logger.LogWarning(BotEventId, $"{e.Command.Name ?? "NULL"} Command Error: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"} by [{e.Context.User}] in [{e.Context.Channel.Name} ({e.Context.Channel.Id})]\nStackTrace:{e.Exception.StackTrace} "); //changes from time to time
            }
            else {
                sender.Client.Logger.LogWarning(BotEventId, $"{e.Context.User} tried to look for {e.Context.Message.Content} in [{e.Context.Channel.Name} ({e.Context.Channel.Id})]");
            }
        }
    }
}
