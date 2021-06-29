using AquacraftBot.Services;
using AquacraftBot.Services.BotServices;
using AquacraftBot.Services.GiveawayServices;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static AquacraftBot.Services.BotServices.BotServices;
using Group = AquacraftBot.Services.Group;
using Timer = System.Timers.Timer;

namespace AquacraftBot.Commands.GiveawayCmds
{
    [Group("giveaway")]
    [Aliases("g")]
    [GroupName(Group.Giveaway)]
    [Description("A giveaway set of commands that allows users to create and start giveaway, reroll, and force end. Along with the default `w!giveaway` " +
        "showing a tutorial on how to start a giveaway")]
    public class GiveawayCmd : BaseCommandModule
    {
        [GroupCommand, Description("Giveaway starter tutorial on how to start, end and reroll a giveaway.")]
        [Cooldown(1,3, CooldownBucketType.Channel)]
        public async Task giveaway(CommandContext ctx)
        {
            // Basically just sends a string of messages or a whole emebd to show how to set up giveaways
            StringBuilder tutorial = new StringBuilder();
            tutorial.AppendLine("Hi there! Welcome to the giveaway options of the WalroosBot. Let's set up your giveaway!");
            tutorial.AppendLine($":boom: You could use `{GlobalData.prefixes[0]}giveaway start|create` with or without arguments,");
            tutorial.AppendLine($"as both have overloads, one is an interactive and another is a default set command. Examples:");
            tutorial.AppendLine($"`{GlobalData.prefixes[0]}giveaway start`");
            tutorial.AppendLine($"`{GlobalData.prefixes[0]}giveaway create`");
            tutorial.AppendLine($"`{GlobalData.prefixes[0]}giveaway start [duration] [amntOfWinners] [gChannel] [PrizeTitle]` ie.");
            tutorial.AppendLine($"`{GlobalData.prefixes[0]}giveaway start 1w:3d:5h:30m 1 #giveaways Awesome T-Shirt Design` \u2190 Starts a giveaway with duration `1 week 3 days 5 hours and 30 minutes` with one winner, in `#giveaways` channel.");
            await ctx.RespondAsync(tutorial.ToString()).ConfigureAwait(false);
        }
        #region GiveawayMaking
        /*
         * Starts off by using making a giveaway model and uploading it to the database and starts a timer,
         * then sends a GiveawayEmbed from services with the remaining timespan on show, and reacting in the message.
         * Replies with Giveaway has started in 
         */
        [Command("start"), Description("Starts a giveaway. Do `w!giveaway` for more information")]
        [Aliases("create")]
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task startInteractive(CommandContext ctx)
        {
            // Params as if simply settings
            string durationString = string.Empty; // done
            int winners = 0; // done
            DiscordChannel channel = null; // done
            string PrizeTitle = string.Empty;
            string inputBuffer = string.Empty;
            string gID = Guid.NewGuid().ToString().Substring(0, 8); // done
            var interactive = ctx.Client.GetInteractivity();

            // part 1
            string msgSent = $":dancer: Aighty! Let's set up your giveaway! First up, what channel would you like your giveaway in?\n" +
                $"You can type `cancel` at any input point to cancel creation.\n\n`Please type the name/mentioning/id the name of a channel in this server.`";
            await ctx.Channel.SendMessageAsync(msgSent).ConfigureAwait(false);
            do {
                var result = await interactive.WaitForMessageAsync(x=>x.Author == ctx.User && x.Channel == ctx.Channel).ConfigureAwait(false);                
                channel = result.Result.MentionedChannels[0];
                inputBuffer = result.Result.Content;
                if (inputBuffer.ToLower().Contains("cancel"))
                    break;
                if (channel == null)
                {
                    var m = await ctx.Channel.SendMessageAsync("I can't seem to find Channels from your message there, try again!").ConfigureAwait(false);
                    await Task.Delay(3000);
                    await result.Result.DeleteAsync();
                    await m.DeleteAsync();
                }                 
             
            } while (channel == null);
            if (inputBuffer.ToLower().Contains("cancel"))
                return;

            // part 2
            msgSent = $":boom: Great! The giveaway will be in {channel.Mention}! Next up, how long should the giveaway last?\n\n" +
                $"`Please enter the duration of the giveaway in seconds.\nFormat of duration: 1w 10d 20h 30m 30s means 1 week, 10 days, 20 hours, 30 minutes, and 30 seconds.`";
            await ctx.Channel.SendMessageAsync(msgSent).ConfigureAwait(false);
            Regex rx = new Regex(@"((?<number>\d+(?:[.,]\d+)?)(?<letter>[wdhms]))+", RegexOptions.IgnoreCase);            
            do
            {
                var result = await interactive.WaitForMessageAsync(x => x.Author == ctx.User && x.Channel == ctx.Channel).ConfigureAwait(false);
                durationString = result.Result.Content;
                inputBuffer = result.Result.Content;
                if (inputBuffer.ToLower().Contains("cancel"))
                    break;
                if (!rx.IsMatch(durationString))
                {
                    var m = await ctx.Channel.SendMessageAsync("Sorry, I don't seem to understand that input, please try again!").ConfigureAwait(false);
                    await Task.Delay(3000);
                    await result.Result.DeleteAsync();
                    await m.DeleteAsync();
                }                
            } while (durationString == string.Empty || (rx.IsMatch(durationString) == false));
            if (inputBuffer.ToLower().Contains("cancel"))
                return;
            TimeSpan day = TimeSpan.Zero, hours = TimeSpan.Zero, minutes = TimeSpan.Zero, seconds = TimeSpan.Zero;
            var matches = rx.Matches(durationString)
                    .Cast<Match>()
                    .Where(m => m.Groups["number"].Success && m.Groups["letter"].Success)
                    .ToList();
            foreach (Match match in matches)
            {
                switch (match.Groups["letter"].ToString().ToLower())
                {
                    case "w": // Basically just adds days to it from number of weeks
                        day += TimeSpan.FromDays(double.Parse(match.Groups["number"].ToString()) * 7);
                        break;
                    case "d":
                        day += TimeSpan.FromDays(double.Parse(match.Groups["number"].ToString()));
                        break;
                    case "h":
                        hours += TimeSpan.FromHours(double.Parse(match.Groups["number"].ToString()));
                        break;
                    case "m":
                        minutes += TimeSpan.FromMinutes(double.Parse(match.Groups["number"].ToString()));
                        break;
                    case "s":
                        seconds += TimeSpan.FromSeconds(double.Parse(match.Groups["number"].ToString()));
                        break;
                    default:
                        break;
                }
            }
            TimeSpan durationTime = day.Add(hours.Add(minutes.Add(seconds)));
            DateTime endTime = DateTime.UtcNow + durationTime;

            // part 3
            StringBuilder timeString = new StringBuilder();
            int weeks = (int)durationTime.TotalDays / 7;
            if (weeks > 0) // There is weeks
            { timeString.Append($"**{weeks}** week(s), **{Math.Abs((7*weeks) - durationTime.Days)}** {(durationTime.Days == 1 ? "day" : "days")}, "); }
            else // There is days
            { timeString.Append($"**{durationTime.Days}** {(durationTime.Days == 1 ? "day" : "days")}, "); }
            if (durationTime.Hours > 0) // There is hours
            { timeString.Append($"**{durationTime.Hours}** {(durationTime.Hours == 1 ? "hour" : "hours")}, "); }
            if (durationTime.Minutes > 0) // There is minutes
            { timeString.Append($"**{durationTime.Minutes}** {(durationTime.Minutes == 1 ? "minute" : "minutes")}, "); }
            if (durationTime.Seconds > 0) // There is seconds
            { timeString.Append($"**{durationTime.Seconds}** {(durationTime.Seconds == 1 ? "second" : "seconds")} "); }
            msgSent = $":man_dancing: Neat! The giveaway will last {timeString}! How many winners would you like to have?\n\n`Please enter a number of winners between 1 and 20.`";
            await ctx.Channel.SendMessageAsync(msgSent).ConfigureAwait(false);
            do {
                var result = await interactive.WaitForMessageAsync(x => x.Author == ctx.User && x.Channel == ctx.Channel).ConfigureAwait(false);
                inputBuffer = result.Result.Content;
                if (inputBuffer.ToLower().Contains("cancel"))
                    break;
                if (!int.TryParse(inputBuffer,out winners))
                {
                    var m = await ctx.Channel.SendMessageAsync("Sorry, I don't seem to understand that input. It has to be a number, please try again!").ConfigureAwait(false);
                    await Task.Delay(3000);
                    await result.Result.DeleteAsync();
                    await m.DeleteAsync();
                }                
            } while (winners == 0);
            if (inputBuffer.ToLower().Contains("cancel"))
                return;

            // part 4
            msgSent = $":confetti_ball: Nice! **{winners}** {(winners == 1 ? "winner" : "winners")} it is! Lastly, what would you like to give away?\n\n`Please enter the giveaway prize. Please note that this will begin the giveaway.`";
            await ctx.Channel.SendMessageAsync(msgSent).ConfigureAwait(false);
            do {
                var result = await interactive.WaitForMessageAsync(x => x.Author == ctx.User && x.Channel == ctx.Channel).ConfigureAwait(false);
                inputBuffer = result.Result.Content;
                PrizeTitle = result.Result.Content;
                if (inputBuffer.ToLower().Contains("cancel"))
                    break;
            } while (PrizeTitle == string.Empty);
            if (inputBuffer.ToLower().Contains("cancel"))
                return;

            // Starting giveaway
            GiveawayModel gItem = new GiveawayModel()
            {
                gID = gID,
                PrizeTitle = PrizeTitle,
                Winners = winners,
                HosterID = ctx.User.Id,
                channelID = channel.Id,
                EndAt = Timestamp.FromDateTime(endTime),
                Ended = false
            };
            var gEmbed = await GiveawayService.giveawayEmbedAsync(ctx.Client, gItem);

            var tadaEmote = DiscordEmoji.FromName(ctx.Client, ":tada:");
            var gMsg = await channel.SendMessageAsync(":confetti_ball: **GIVEAWAY** :confetti_ball:", gEmbed).ConfigureAwait(false);
            await gMsg.CreateReactionAsync(tadaEmote).ConfigureAwait(false);

            gItem.messageID = gMsg.Id;
            DocumentReference docRef = GlobalData.database.Collection("Giveaways").Document(gID);
            await docRef.SetAsync(gItem);

            // Starts a timer and add to Global Dictionary
            Timer gTimer = new Timer((endTime - DateTime.UtcNow).TotalMilliseconds);
            gTimer.Elapsed += (source, e) => GiveawayService.GiveawayEndedEvent(source, e, gItem);
            gTimer.AutoReset = false;
            gTimer.Start();
            Console.WriteLine($"Giveaway #{gID} timer has started! Will end in {(endTime - DateTime.UtcNow):g}");
            GlobalData.GiveawayTimers.Add(gItem.gID, gTimer);
            
            await ctx.Channel.SendMessageAsync($":tada: Done! Giveaway #**{gItem.gID}** has started in {channel.Mention}, " +
                $"hosted by {ctx.User.Mention}({gItem.HosterID}), for `{gItem.PrizeTitle}` and will end at {gItem.EndAt.ToDateTime():F} {gItem.EndAt.ToDateTime().Kind.ToString().ToUpper()}. :confetti_ball:").ConfigureAwait(false);
        }

        [Command("start"), Description("Starts a giveaway. Do `w!giveaway` for more information")]     
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task startSet(CommandContext ctx, [Description("Duration of the giveaway (using `:` as spaces.)")] string duration, 
            [Description("Number of winners")] int winners,
            [Description("Channel to set up the giveaway (defaults to command set channel)")] DiscordChannel channel = null,
            [RemainingText][Description("The Prize")] string PrizeTitle = "Exciting Prize!")
        {
            // Gets all the variables 
            if (channel == null)
                channel = ctx.Channel;
            string gID = Guid.NewGuid().ToString().Substring(0, 8);
            TimeSpan day = TimeSpan.Zero, hours = TimeSpan.Zero, minutes= TimeSpan.Zero, seconds= TimeSpan.Zero;
            // Gets the timespan from the duration string
            Regex rx = new Regex(@"((?<number>\d+(?:[.,]\d+)?)(?<letter>[wdhms]))+", RegexOptions.IgnoreCase);
            duration = duration.ToLower();
            duration = duration.Replace(":", " ");
            if (rx.IsMatch(duration))
            {                
                var matches = rx.Matches(duration)
                    .Cast<Match>()
                    .Where(m => m.Groups["number"].Success && m.Groups["letter"].Success)
                    .ToList();
                foreach (Match match in matches)
                {
                    switch (match.Groups["letter"].ToString().ToLower())
                    {
                        case "w": // Basically just adds days to it from number of weeks
                            day += TimeSpan.FromDays(double.Parse(match.Groups["number"].ToString()) * 7);
                            break;
                        case "d":
                            day += TimeSpan.FromDays(double.Parse(match.Groups["number"].ToString()));
                            break;
                        case "h":
                            hours += TimeSpan.FromHours(double.Parse(match.Groups["number"].ToString()));
                            break;
                        case "m":
                            minutes += TimeSpan.FromMinutes(double.Parse(match.Groups["number"].ToString()));
                            break;
                        case "s":
                            seconds += TimeSpan.FromSeconds(double.Parse(match.Groups["number"].ToString()));
                            break;
                        default:
                            break;
                    }
                } 
            } else // Duration does not match with the regex
            {
                var mesg = await ctx.RespondAsync("The duration input seems to be invalid. You can only enter in `w`eeks, `d`ays, and `h`ours. Please try again.").ConfigureAwait(false);
                await Task.Delay(4000);
                await mesg.DeleteAsync().ConfigureAwait(false);
                return;
            }
            TimeSpan durationTime = day.Add(hours.Add(minutes.Add(seconds)));
            DateTime endTime = DateTime.UtcNow + durationTime;
            GiveawayModel gItem = new GiveawayModel()
            {
                gID = gID,
                PrizeTitle = PrizeTitle,
                Winners = winners,
                HosterID = ctx.User.Id,
                channelID = channel.Id,
                EndAt = Timestamp.FromDateTime(endTime),
                Ended = false
            };
            var gEmbed = await GiveawayService.giveawayEmbedAsync(ctx.Client, gItem);

            var tadaEmote = DiscordEmoji.FromName(ctx.Client, ":tada:");
            var gMsg = await channel.SendMessageAsync(":confetti_ball: **GIVEAWAY** :confetti_ball:",gEmbed).ConfigureAwait(false);
            await gMsg.CreateReactionAsync(tadaEmote).ConfigureAwait(false);

            gItem.messageID = gMsg.Id;
            DocumentReference docRef = GlobalData.database.Collection("Giveaways").Document(gID);
            await docRef.SetAsync(gItem);

            // Starts a timer and add to Global Dictionary
            Timer gTimer = new Timer((endTime - DateTime.UtcNow).TotalMilliseconds);
            gTimer.Elapsed += (source, e) => GiveawayService.GiveawayEndedEvent(source, e, gItem);
            gTimer.AutoReset = false;
            gTimer.Start();
            Console.WriteLine($"Giveaway #{gID} timer has started! Will end in {(endTime - DateTime.UtcNow):g}");
            GlobalData.GiveawayTimers.Add(gItem.gID, gTimer);
             await ctx.RespondAsync($":tada: Giveaway #**{gItem.gID}** has started in {channel.Mention}, " +
                $"hosted by {ctx.User.Mention}({gItem.HosterID}), for `{gItem.PrizeTitle}` will end at {gItem.EndAt.ToDateTime():F} {gItem.EndAt.ToDateTime().Kind.ToString().ToUpper()}. :confetti_ball:").ConfigureAwait(false);
        }

        #endregion GiveawayMaking

        #region GiveawayEnding
        [Command("end"), Description("Force ends a giveaway and rolls a winner")]
        [GroupName(Group.Giveaway)]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task forceEnd(CommandContext ctx, [Description("Giveaway ID to end (gID given when making a Giveaway)")] string giveawayID)
        {
            DocumentReference gRef = GlobalData.database.Collection("Giveaways").Document(giveawayID);
            DocumentSnapshot gSnap = await gRef.GetSnapshotAsync().ConfigureAwait(false);
            await ctx.Message.DeleteAsync().ConfigureAwait(false);
            if (gSnap.Exists)
            {
                // Ends the timer, instantly deletes the database information on the giveaway               
                var gItem = gSnap.ConvertTo<GiveawayModel>();
                var client = ctx.Client;
                var gChannel = await client.GetChannelAsync(gItem.channelID).ConfigureAwait(false);
                var gMsg = await gChannel.GetMessageAsync(gItem.messageID).ConfigureAwait(false);

                var tadaEmote = DiscordEmoji.FromName(client, ":tada:");
                var reactions = await gMsg.GetReactionsAsync(tadaEmote, 200).ConfigureAwait(false);
                if (reactions.Count - 1 < gItem.Winners)
                {
                    await gMsg.DeleteAsync().ConfigureAwait(false);
                    await gChannel.SendMessageAsync($"Oopsie, not enough people participated to Giveaway **#{gItem.gID}** >~< and so no one won **{gItem.PrizeTitle}**.").ConfigureAwait(false);

                    GlobalData.GiveawayTimers[gItem.gID].Stop();
                    GlobalData.GiveawayTimers[gItem.gID].Dispose();
                    DocumentReference geRef = GlobalData.database.Collection("Giveaways").Document(gItem.gID);
                    await geRef.DeleteAsync().ConfigureAwait(false);
                    return;
                }

                var hoster = await client.GetUserAsync(gItem.HosterID);
                List<DiscordUser> winners = new List<DiscordUser>();
                for (int w = 0; w < gItem.Winners; w++)
                {
                    DiscordUser winner = null;
                    do {
                        winner = await GiveawayService.getWinnerAsync(client, gMsg).ConfigureAwait(false);
                    } while (winners.Contains(winner));
                    winners.Add(winner);
                }
                var gEmbed = GiveawayService.endedEmbedAsync(gItem, winners, hoster);

                var pinata = DiscordEmoji.FromName(client, ":piñata:");
                await gMsg.ModifyAsync($"{pinata} **GIVEAWAY ENDED** {pinata}", gEmbed.Build()).ConfigureAwait(false);
                var emote = DiscordEmoji.FromName(ctx.Client, ":koi:");
                string EndingMsg = $"Congratulations {string.Join(",",winners.Select(w=>w.Mention))}! You won **{gItem.PrizeTitle}**! {emote}:\n{gMsg.JumpLink}";
                await gChannel.SendMessageAsync(EndingMsg).ConfigureAwait(false);

                GlobalData.GiveawayTimers[gItem.gID].Stop();
                GlobalData.GiveawayTimers[gItem.gID].Dispose();
                Console.WriteLine($"Giveaway #{gItem.gID} has been forcefully ended!");
                gItem.Ended = true;
                await gRef.SetAsync(gItem);
            }
            else
                await BotServices.SendEmbedAsync(ctx,"Giveaway Not Found", "Sorry that giveaway is not ongoing right now or does not exist. If this is a mistake, please contact developer through `w!info`"
                    ,ResponseType.Missing).ConfigureAwait(false);
        
        }

        [Command("cancel"), Description("Cancels a giveaway without rolling a winner")]
        [GroupName(Group.Giveaway)]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task cancel(CommandContext ctx, [Description("Giveaway ID to cancel (gID given when making a Giveaway)")] string giveawayID)
        {
            DocumentReference gRef = GlobalData.database.Collection("Giveaways").Document(giveawayID);
            DocumentSnapshot gSnap = await gRef.GetSnapshotAsync().ConfigureAwait(false);
            await ctx.Message.DeleteAsync().ConfigureAwait(false);
            if (gSnap.Exists)
            {
                // Ends the timer, instantly deletes the database information on the giveaway               
                var gItem = gSnap.ConvertTo<GiveawayModel>();
                var client = ctx.Client;
                var gChannel = await client.GetChannelAsync(gItem.channelID).ConfigureAwait(false);
                var gMsg = await gChannel.GetMessageAsync(gItem.messageID).ConfigureAwait(false);
                 
                var hoster = await client.GetUserAsync(gItem.HosterID);
                var canceller = ctx.User;
                var gEmbed = GiveawayService.cancelledEmbed(gItem, hoster, canceller);

                await gMsg.ModifyAsync($"**GIVEAWAY CANCELLED**", gEmbed.Build()).ConfigureAwait(false);

                string EndingMsg = $"We're sorry >~< ! Giveaway for **{gItem.PrizeTitle}** has been cancelled!\n{gMsg.JumpLink}";
                await gChannel.SendMessageAsync(EndingMsg).ConfigureAwait(false);

                GlobalData.GiveawayTimers[gItem.gID].Stop();
                GlobalData.GiveawayTimers[gItem.gID].Dispose();
                Console.WriteLine($"Giveaway #{gItem.gID} has been cancelled!");
                await gRef.DeleteAsync().ConfigureAwait(false);               
            }
            else
                await BotServices.SendEmbedAsync(ctx, "Giveaway Not Found", "Sorry that giveaway is not ongoing right now or does not exist. If this is a mistake, please contact developer through `w!info`"
                    , ResponseType.Missing).ConfigureAwait(false);
        }
        #endregion GiveawayEnding

        #region GiveawayUtil
        [Command("reroll"), Description("Simple rerolling the winners according to the number of winners set")]
        public async Task reroll(CommandContext ctx, string giveawayID) 
        {
            DocumentReference gRef = GlobalData.database.Collection("Giveaways").Document(giveawayID);
            DocumentSnapshot gSnap = await gRef.GetSnapshotAsync().ConfigureAwait(false);
            await ctx.Message.DeleteAsync().ConfigureAwait(false);
            if (gSnap.Exists)
            {                     
                var gItem = gSnap.ConvertTo<GiveawayModel>();
                if (gItem.Ended)
                {
                    var client = ctx.Client;
                    var gChannel = await client.GetChannelAsync(gItem.channelID).ConfigureAwait(false);
                    var gMsg = await gChannel.GetMessageAsync(gItem.messageID).ConfigureAwait(false);

                    var hoster = await client.GetUserAsync(gItem.HosterID);
                    List<DiscordUser> winners = new List<DiscordUser>();
                    for (int w = 0; w < gItem.Winners; w++)
                    {
                        DiscordUser winner = null;
                        do
                        {
                            winner = await GiveawayService.getWinnerAsync(client, gMsg).ConfigureAwait(false);
                        } while (winners.Contains(winner));
                        winners.Add(winner);
                    }
                    var gEmbed = GiveawayService.endedEmbedAsync(gItem, winners, hoster);

                    var emote = DiscordEmoji.FromName(ctx.Client, ":koi:");
                    var pinata = DiscordEmoji.FromName(ctx.Client, ":piñata:");
                    await gMsg.ModifyAsync($"{pinata} **GIVEAWAY ENDED** {pinata}", gEmbed.Build()).ConfigureAwait(false);
                    string EndingMsg = $"Congratulations, the new winners are {string.Join(",", winners.Select(w => w.Mention))}! You won **{gItem.PrizeTitle}**! {emote}\n{gMsg.JumpLink}";
                    await gChannel.SendMessageAsync(EndingMsg).ConfigureAwait(false);

                    GlobalData.GiveawayTimers[gItem.gID].Stop();
                    GlobalData.GiveawayTimers[gItem.gID].Dispose();
                    Console.WriteLine($"Giveaway #{gItem.gID} has been rerolled!");
                }
                else
                    await BotServices.SendEmbedAsync(ctx, "Giveaway Error", "Sorry, that giveaway has not ended, and such cannot be rerolled. If this is a mistake, please contact developer through `w!info`", ResponseType.Default)
                        .ConfigureAwait(false);
            }
            else
                await BotServices.SendEmbedAsync(ctx, "Giveaway Not Found", "Sorry that giveaway is not ongoing right now or does not exist. If this is a mistake, please contact developer through `w!info`"
                    ,ResponseType.Missing).ConfigureAwait(false);
        }

        [Command("updater"), Description("Sets up an updater for updating the GiveawayEmbeds on the Time Remaining field")]
        [GroupName(Group.Giveaway)]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task updater(CommandContext ctx)
        {
            if (!GlobalData.GiveawayClientUpdater.Item1)
            {
                GlobalData.GiveawayClientUpdater = new Tuple<bool, DiscordClient>(true, ctx.Client);
            }
            else
                GlobalData.GiveawayClientUpdater = new Tuple<bool, DiscordClient>(false, null);
            await ctx.RespondAsync($"GEmbed Updater has been {((GlobalData.GiveawayClientUpdater.Item1 == true) ? "Enabled" : "Disabled")}").ConfigureAwait(false);
        }
        #endregion GiveawayUtil
    }
}
