using DSharpPlus;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace AquacraftBot.Services.GiveawayServices
{
    public class GiveawayService
    {
#region gEmbeds
        public static async Task<DiscordEmbedBuilder> giveawayEmbedAsync(DiscordClient client, GiveawayModel giveawayItem)
        {
            // Needed variables such as hoster user and timestamp convert
            var hoster = await client.GetUserAsync(giveawayItem.HosterID);
            DateTime endingDateTime = giveawayItem.EndAt.ToDateTime(); // Date time in UTC, Make sure that other giveaway stamps is in UTC
            var emote = DiscordEmoji.FromName(client, ":tada:");

            string desc = $"\u2022 {giveawayItem.Winners} winner(s)\n\u2022 Hosted by {hoster.Mention}";

            var embed = new DiscordEmbedBuilder()
                .WithTitle(giveawayItem.PrizeTitle)
                .WithColor(GlobalData.defaultColour)
                .WithDescription(desc)
                .WithTimestamp(endingDateTime)
                .WithFooter($"gID: {giveawayItem.gID} | Ends at");

            // Time remaining timespan
            var remaining = endingDateTime - DateTime.UtcNow;
            StringBuilder value = new StringBuilder();
            value.Append("Time remaining: ");

            int weeks = (int)remaining.TotalDays / 7;
            if (weeks > 0) // There is weeks
            { value.Append($"**{weeks}** week(s), **{Math.Abs((7*weeks) - remaining.Days)}** {(remaining.Days == 1 ? "day" : "days")}, "); }
            else
            { value.Append($"**{remaining.Days}** {(remaining.Days == 1? "day" : "days")}, " ); }
            if (remaining.Hours > 0) // There is hours
            { value.Append($"**{remaining.Hours}** {(remaining.Hours == 1? "hour" : "hours")}, "); }
            if (remaining.Minutes > 0) // There is minutes
            { value.Append($"**{remaining.Minutes}** {(remaining.Minutes == 1? "minute" : "minutes")}, "); }
            if (remaining.Seconds > 0) // There is seconds
            { value.Append($"**{remaining.Seconds}** {(remaining.Seconds == 1? "second" : "seconds")} "); }

            embed.AddField($"React with {emote} to enter the giveaway!", value.ToString());

            return embed;
        }

        /// <summary>
        /// Get Giveaway Ended Embed to replace GiveawayEmbed for more than one winner
        /// </summary>
        /// <param name="giveawayItem"> Giveaway model from the database </param>
        /// <param name="winners">Loop through the list and write in the description</param>
        /// <param name="hoster"> Hoster Discord user to mention in the embed</param>
        /// <returns></returns>
        public static DiscordEmbedBuilder endedEmbedAsync(GiveawayModel giveawayItem, List<DiscordUser> winners, DiscordUser hoster) // For a single winner
        {
            // This will basically just return a winning embed with PrizeTitle, Ending Timestamp, Winner mention and hoster mention, 
            // with no colour and no modifying giveaway message <-- will be done in the command
            var endedTime = giveawayItem.EndAt.ToDateTime();
            var desc = $"Winner: {string.Join(",", winners.Select(x => x.Mention))}\n" +
                $"Hosted by: {hoster.Mention}";

            var embed = new DiscordEmbedBuilder()
                .WithTitle(giveawayItem.PrizeTitle)
                .WithTimestamp(endedTime)
                .WithDescription(desc)
                .WithFooter($"gID: {giveawayItem.gID} | Ended At");

            return embed;
        }

        /// <summary>
        /// Get Embed to edit gEmbed for when a giveaway is cancelled
        /// </summary>
        /// <param name="giveawayitem"> Giveaway Item Info to be used</param>
        /// <param name="hoster">To Show on Embed who hosted</param>
        /// <param name="canceller">To show on Embed who cancelled the giveaway </param>>
        /// <returns></returns>
        public static DiscordEmbedBuilder cancelledEmbed(GiveawayModel giveawayitem, DiscordUser hoster, DiscordUser canceller)
        {
            // Cancel Embed is similar to Ended Embed, but instead of winner it shows who cancelled
            var desc = $"Cancelled by: {canceller.Mention}\n" +
                $"Hosted by: {hoster.Mention}";

            var embed = new DiscordEmbedBuilder()
                .WithTitle(giveawayitem.PrizeTitle)
                .WithTimestamp(DateTime.Now)
                .WithDescription(desc)
                .WithFooter($"gID: {giveawayitem.gID}");

            return embed;

        }
        #endregion gEmbeds

        /// <summary>
        /// Get Giveaway Winner from a giveaway item from msg (make sure to loop this method as much as winners as there are in the gModel)
        /// </summary>
        /// <param name="client"> Discord client to use to get channel and message for rolling winner </param>
        /// <param name="giveawayItem"> Giveaway model Item to get database information </param>
        /// <returns></returns>
        public static async Task<DiscordUser> getWinnerAsync(DiscordClient client, DiscordMessage giveawayMsg)
        {
            // Gets the reactions from the message and then gets the reactions and gets the users from the reaction and roll it
            var tadaEmote = DiscordEmoji.FromName(client, ":tada:");
            var gUsers = await giveawayMsg.GetReactionsAsync(tadaEmote, 200).ConfigureAwait(false);
            var users = gUsers.ToList();            
            users.Remove(client.CurrentUser);
            Console.WriteLine("Users found in Giveaway: {0}", users.Count);
            var winnerIndex = new Random().Next(0, users.Count);
            var winner = users[winnerIndex];

            return winner;
        }

        #region gEventHandlers
        /// <summary>
        /// Elapsed Event for when the Timer has passed for a Giveaway. Basically when it ends
        /// </summary>
        /// <param name="source"> Idk what this is lol</param>
        /// <param name="e"> The event args object for the event</param>
        /// <param name="giveawayRef"> GItem used for the data for things to end the giveaway </param>
        public static async void GiveawayEndedEvent(object source, ElapsedEventArgs e,GiveawayModel gItem)
        {
            /*
             * Basically will get the message and rolls the winner and replace the embed with a giveaway embed
             */
            var client = GlobalData.GiveawayClientUpdater.Item2;
            var gChannel = await client.GetChannelAsync(gItem.channelID);
            var gGuild = gChannel.Guild;
            var gMsg = await gChannel.GetMessageAsync(gItem.messageID);

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
                    winner = await getWinnerAsync(client, gMsg).ConfigureAwait(false);
                } while (winners.Contains(winner));
                winners.Add(winner);              
            }
            var gEmbed = endedEmbedAsync(gItem, winners, hoster);

            await gMsg.ModifyAsync($":piñata: **GIVEAWAY ENDED** :piñata:", gEmbed.Build()).ConfigureAwait(false);
            string EndingMsg = $"Congratulations {string.Join(",", winners.Select(w=>w.Mention))}! You won **{gItem.PrizeTitle}**! :koi:\n{gMsg.JumpLink}";
            await gChannel.SendMessageAsync(EndingMsg).ConfigureAwait(false);

            GlobalData.GiveawayTimers[gItem.gID].Stop();
            GlobalData.GiveawayTimers[gItem.gID].Dispose();
            gItem.Ended = true;
            DocumentReference gRef = GlobalData.database.Collection("Giveaways").Document(gItem.gID);
            await gRef.SetAsync(gItem);

            // Tries to DM the user only if it is possible;; the below sendDMEmbed method has try catch 
            try
            {
                foreach (var winner in winners)
                {
                    var member = await gGuild.GetMemberAsync(winner.Id);
                    await BotServices.BotServices.SendDMEmbedAsync(member, "Congratulations! You won a giveaway! :tada:",
                        $"Congrats, you won **{gItem.PrizeTitle}**! The giveaway was hosted by {hoster.Mention}, in {gChannel.Mention}. Be sure to thank them! :ribbon:",
                        gChannel).ConfigureAwait(false);
                }
            } catch 
            { }
        }

        /// <summary>
        /// Async Event Handler that updates Embeds for Time Remaining
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public static async void GEmbedUpdateEvent(object source, ElapsedEventArgs e)
        {
            // Using a global client to get the message per reference
            Query allGItems = GlobalData.database.Collection("Giveaways");
            QuerySnapshot GItemSnaps = await allGItems.GetSnapshotAsync().ConfigureAwait(false);

            foreach (DocumentSnapshot GItemSnap in GItemSnaps)
            {
                var GItem = GItemSnap.ConvertTo<GiveawayModel>();
                if (GlobalData.GiveawayClientUpdater.Item1)
                {
                    if (!GItem.Ended)
                    {
                        var discClient = GlobalData.GiveawayClientUpdater.Item2;
                        var gChannel = await discClient.GetChannelAsync(GItem.channelID);
                        var gMsg = await gChannel.GetMessageAsync(GItem.messageID);

                        if ((GItem.EndAt.ToDateTime() - DateTime.UtcNow) < TimeSpan.Zero)
                        { continue; }

                        // Gets a new embed using the gEmbed Method and Modifies the message
                        var gEmbed = await giveawayEmbedAsync(discClient, GItem);
                        await gMsg.ModifyAsync(":confetti_ball: **GIVEAWAY** :confetti_ball:", gEmbed.Build()).ConfigureAwait(false);
                        Console.WriteLine($"GEmbed for Giveaway #{GItem.gID} has been updated!");
                    }
                }
            }
        }
        #endregion gEventHandlers

    }
}   