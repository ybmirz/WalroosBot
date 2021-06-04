using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Net.Models;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquacraftBot.Services.TickettingServices
{
    public class TicketterServices
    {
        #region InitiateTicketEmbed
        // Send starting embed and reacting to it
        public static async Task<DiscordMessage> SendTicketInitiationEmbed(DiscordClient client,DiscordGuild guild, DiscordChannel ticket)
        {
            DiscordEmbedBuilder startingEmbed = TicketEmbed();            
            // Having trouble with getting the tickettool's msg so will just get the whole channel msg and find ticket tool as the one messaging
            var ticketMsgs = await ticket.GetMessagesAsync(5).ConfigureAwait(false);
            DiscordMessage ticketToolMsg = null;
            foreach (var msg in ticketMsgs)
            {
                if (msg.Author.Id == 557628352828014614)
                    ticketToolMsg = msg;
            }

            DiscordUser ticketRequester = null;
            DiscordMember ticketMember = null;
            if (ticketToolMsg != null)
            {
                ticketRequester = ticketToolMsg.MentionedUsers[0];
                startingEmbed.WithFooter($"Requested by {ticketRequester.Username}#{ticketRequester.Discriminator}");
                startingEmbed.WithThumbnail(ticketRequester.AvatarUrl);
                ticketMember = await guild.GetMemberAsync(ticketRequester.Id);
            }

            List<DiscordEmoji> emojiList = new List<DiscordEmoji>();
            // Add Field Report Player
            var playerReport = DiscordEmoji.FromName(client, ":exclamation:");
            startingEmbed.AddField($"Report a Player", $"If you would like to report a player, click on {playerReport} below. Player reports range from griefing/stealing and generally a player that you'd like staff to look upon.");
            emojiList.Add(playerReport);

            // Add Field Report Bug
            var bugReport = DiscordEmoji.FromName(client, ":space_invader:");
            startingEmbed.AddField($"Report a Bug", $"If you would like to report a bug, whether it is a game/map/building bug, click on {bugReport} below. We'll be with you shortly.");
            emojiList.Add(bugReport);

            // Add Field Punishment Appeal
            var appeal = DiscordEmoji.FromName(client, ":scroll:");
            startingEmbed.AddField($"Punishment Appeal", $"If you would like to appeal for a punishment, whether it is a ban/mute/warn, click on {appeal} below. We'll be with you shortly.");
            emojiList.Add(appeal);

            // Add Field Redeem Giveaways or Prizes
            var prize = DiscordEmoji.FromName(client, ":tada:");
            startingEmbed.AddField("Redeem Giveaways/Prizes", $"Click on {prize} below, if you would like to redeem giveaways or prizes partaining to the server. We'll be with you shortly.");
            emojiList.Add(prize);

            // Add Field General Support Ticket
            var general = DiscordEmoji.FromName(client, ":mailbox_with_mail:");
            startingEmbed.AddField($"General Support Ticket", $"If your ticket is not within the above options, click on {general} below. This ticket will be a general support question, please do not abuse. We'll be with you shortly.");
            emojiList.Add(general);

            // Temp: Build Entry Ticket
            var build = DiscordEmoji.FromName(client, ":homes:");
            startingEmbed.AddField("Build Entry Ticket", $"Temporary ticket option for those that would like to submit a build entry in the ongoing June Build Event. Please do not choose this option to simply `check` if it works. If you'd like to submit a build entry, click on {build} below.");
            emojiList.Add(build);

            var initiateMsg = await ticket.SendMessageAsync(ticketRequester.Mention,startingEmbed.Build()).ConfigureAwait(false);
            
            await initiateMsg.CreateReactionAsync(playerReport).ConfigureAwait(false);
            await initiateMsg.CreateReactionAsync(bugReport).ConfigureAwait(false);
            await initiateMsg.CreateReactionAsync(appeal).ConfigureAwait(false);
            await initiateMsg.CreateReactionAsync(prize).ConfigureAwait(false);
            await initiateMsg.CreateReactionAsync(general).ConfigureAwait(false);

            await initiateMsg.CreateReactionAsync(build).ConfigureAwait(false);


            /* Interactive wait for reaction doesnt seem to work, so I'll just take the emoojis from the msg and check which one the requester made*/
            // Update: this method will just simply return the message when it sends
            return initiateMsg;
        }

        // Sends an example embed when the command is done
        public static async Task SendExampleEmbed(CommandContext ctx)
        {
            DiscordEmbedBuilder startingEmbed = TicketEmbed();
            startingEmbed.WithFooter($"Requested by {ctx.User.Username}#{ctx.User.Discriminator}");
            startingEmbed.WithThumbnail(ctx.User.AvatarUrl);

            // Add Field Report Player
            var playerReport = DiscordEmoji.FromName(ctx.Client, ":exclamation:");
            startingEmbed.AddField($"Report a Player", $"If you would like to report a player, click on {playerReport} below. Player reports range from griefing/stealing and generally a player that you'd like staff to look upon.");

            // Add Field Report Bug
            var bugReport = DiscordEmoji.FromName(ctx.Client, ":space_invader:");
            startingEmbed.AddField($"Report a Bug", $"If you would like to report a bug, whether it is a game/map/building bug, click on {bugReport} below. We'll be with you shortly.");

            // Add Field Punishment Appeal
            var appeal = DiscordEmoji.FromName(ctx.Client, ":scroll:");
            startingEmbed.AddField($"Punishment Appeal", $"If you would like to appeal for a punishment, whether it is a ban/mute/warn, click on {appeal} below. We'll be with you shortly.");

            // Add Field Redeem Giveaways or Prizes
            var prize = DiscordEmoji.FromName(ctx.Client, ":tada:");
            startingEmbed.AddField("Redeem Giveaways/Prizes", $"Click on {prize} below, if you would like to redeem giveaways or prizes partaining to the server. We'll be with you shortly.");

            // Add Field General Support Ticket
            var general = DiscordEmoji.FromName(ctx.Client, ":mailbox_with_mail:");
            startingEmbed.AddField($"General Support Ticket", $"If your ticket is not within the above options, click on {general} below. This ticket will be a general support question, please do not abuse. We'll be with you shortly.");

            // Temp: Build Entry Ticket
            var build = DiscordEmoji.FromName(ctx.Client, ":homes:");
            startingEmbed.AddField("Build Entry Ticket", $"Temporary ticket option for those that would like to submit a build entry in the ongoing June Build Event. Please do not choose this option to simply `check` if it works. If you'd like to submit a build entry, click on {build} below.");            

            var initiateMsg = await ctx.Channel.SendMessageAsync(startingEmbed.Build()).ConfigureAwait(false);
            await initiateMsg.CreateReactionAsync(playerReport).ConfigureAwait(false);
            await initiateMsg.CreateReactionAsync(bugReport).ConfigureAwait(false);
            await initiateMsg.CreateReactionAsync(appeal).ConfigureAwait(false);
            await initiateMsg.CreateReactionAsync(prize).ConfigureAwait(false);
            await initiateMsg.CreateReactionAsync(general).ConfigureAwait(false);

            await initiateMsg.CreateReactionAsync(build).ConfigureAwait(false);
        }

        private static DiscordEmbedBuilder TicketEmbed()
        {
            var embed = new DiscordEmbedBuilder()
                .WithAuthor($"{GlobalData.botName}", null, GlobalData.logoURL)
                .WithColor(GlobalData.defaultColour)
                .WithTimestamp(DateTime.Now);
            string desc = "Hi there, thank you for creating a ticket!\n" +
                "Please choose one of the options below that is concerning to your ticket in **15 seconds**.\n" +
                "Once you have chosen by reacting to the emojis on this message, Please fill out the form provided. " +
                "Staff will be with you shortly! " +
                "Have a nice day!";
            embed.WithDescription(desc);            

            return embed;
        }
        #endregion InitiateTicketEmbed

        #region TicketOptions
        public static async Task PlayerReportTicketAsync(DiscordChannel ticket)
        {
            string form = "<Gamemode: Required>\n" +
                "<Username: Required>\n" +
                "<Subject: Brief title on report, ie. Minor Griefing/Illegal Flying/x-ray, etc. Required>\n" +
                "<Involved_Users: Users that you are reporting, suspects along with victims, etc. Required>\n" +
                "<Evidence: Optional but Complimentary>\n" +
                "<Other_notes: Optional>";
            var embed = new DiscordEmbedBuilder()
                .WithTimestamp(DateTime.Now)
                .WithColor(GlobalData.defaultColour)
                .WithTitle("Player Report Format")
                .WithDescription(Formatter.BlockCode(form,"xml"));

            await ticket.SendMessageAsync(embed.Build()).ConfigureAwait(false);
            var docRef = GlobalData.database.Collection("Counters").Document("TicketterCounts");
            await docRef.UpdateAsync("PlayersReports", FieldValue.Increment(1));
            var docSnap = await docRef.GetSnapshotAsync();
            int count = docSnap.GetValue<int>("PlayersReports");
            await ticket.ModifyAsync(prop => prop.Name = $"player-report-{count.ToString("D4")}").ConfigureAwait(false);
        }

        public static async Task BugReportTicketAsync(DiscordChannel ticket)
        {
            string form = "<Gamemode: Required>\n" +
                "<Username: Required>\n" +
                "<Bug: Description of the Bug that you have found. Required>\n" +                
                "<Evidence: Optional but Complimentary>\n" +
                "<Other_notes: Notes on players involved, whether it is an abusable bug or harmless bug. Optional>";
            var embed = new DiscordEmbedBuilder()
                .WithTimestamp(DateTime.Now)
                .WithColor(GlobalData.defaultColour)
                .WithTitle("Bug Report Format")
                .WithDescription(Formatter.BlockCode(form, "xml"));

            await ticket.SendMessageAsync(embed.Build()).ConfigureAwait(false);
            var docRef = GlobalData.database.Collection("Counters").Document("TicketterCounts");
            await docRef.UpdateAsync("BugReports", FieldValue.Increment(1));
            var docSnap = await docRef.GetSnapshotAsync();
            int count = docSnap.GetValue<int>("BugReports");
            await ticket.ModifyAsync(prop => prop.Name = $"bug-report-{count.ToString("D4")}").ConfigureAwait(false);
        }

        public static async Task PunishmentAppealTicketAsync(DiscordChannel ticket)
        {
            string form = 
                "<Username: Required>\n" +
                "<Punishment_Type_and_Reason: Required>\n" +
                "<Staff_Member: Who punished you. Required>\n" +
                "<Date: The date you were punished on. Required>\n" +
                "<Appeal: Why should we reduce your punishment/unpunish you?. Required>\n" +
                "<Evidence: Optional>\n" +
                "<Other notes: Optional>";
            var embed = new DiscordEmbedBuilder()
                .WithTimestamp(DateTime.Now)
                .WithColor(GlobalData.defaultColour)
                .WithTitle("Punishment Appeal Format")
                .WithDescription(Formatter.BlockCode(form, "xml"));

            await ticket.SendMessageAsync(embed.Build()).ConfigureAwait(false);
            var docRef = GlobalData.database.Collection("Counters").Document("TicketterCounts");
            await docRef.UpdateAsync("PunishmentAppeal", FieldValue.Increment(1));
            var docSnap = await docRef.GetSnapshotAsync();
            int count = docSnap.GetValue<int>("PunishmentAppeal");
            await ticket.ModifyAsync(prop => prop.Name = $"punishment-appeal-{count.ToString("D4")}").ConfigureAwait(false);
        }

        public static async Task GiveawayRedeem(DiscordChannel ticket)
        {
            string form =
                "<MC_Username: Required (or discord)>\n" +
                "<Discord_Username: Required (or MC)>\n" +
                "<Prize: The prize that you won and how you won it. Required>\n" +
                "<Date: Optional>\n" +
                "<Description: Optional>";
            var embed = new DiscordEmbedBuilder()
                .WithTimestamp(DateTime.Now)
                .WithColor(GlobalData.defaultColour)
                .WithTitle("Prize Redeem Format")
                .WithDescription(Formatter.BlockCode(form, "xml"));

            await ticket.SendMessageAsync(embed.Build()).ConfigureAwait(false);
            var docRef = GlobalData.database.Collection("Counters").Document("TicketterCounts");
            await docRef.UpdateAsync("PrizeRedeem", FieldValue.Increment(1));
            var docSnap = await docRef.GetSnapshotAsync();
            int count = docSnap.GetValue<int>("PrizeRedeem");
            await ticket.ModifyAsync(prop => prop.Name = $"prize-redeem-{count.ToString("D4")}").ConfigureAwait(false);
        }

        public static async Task GeneralSupportTicketAsync(DiscordChannel ticket)
        {
            string form = "<Gamemode: Required>\n" +
                "<Username: Required>\n" +
                "<Subject: Required>\n" +
                "<Date: Optional>\n" +
                "<Description: Required>\n" +
                "<Evidence: Optional>";
            var embed = new DiscordEmbedBuilder()
                .WithTimestamp(DateTime.Now)
                .WithColor(GlobalData.defaultColour)
                .WithTitle("General Support Format")
                .WithDescription(Formatter.BlockCode(form, "xml"));

            await ticket.SendMessageAsync(embed.Build()).ConfigureAwait(false);
            var docRef = GlobalData.database.Collection("Counters").Document("TicketterCounts");
            await docRef.UpdateAsync("GeneralSupport", FieldValue.Increment(1));
            var docSnap = await docRef.GetSnapshotAsync();
            int count = docSnap.GetValue<int>("GeneralSupport");
            await ticket.ModifyAsync(prop => prop.Name = $"support-ticket-{count.ToString("D4")}").ConfigureAwait(false);
        }

        public static async Task BuildEntry(DiscordChannel ticket)
        {
            string form = 
                "<IGN(s): In-Game Name(s)>\n" +
                "<Name_of_Build: Required>\n" +
                "<Build Backstory: Optional>\n" +
                "<Coordinates_of_Build: Required>\n" +
                "<Screenshots_of_Build: Required>";
            var embed = new DiscordEmbedBuilder()
                .WithTimestamp(DateTime.Now)
                .WithColor(GlobalData.defaultColour)
                .WithTitle("Build Entry Format")
                .WithDescription(Formatter.BlockCode(form, "xml"));

            await ticket.SendMessageAsync(embed.Build()).ConfigureAwait(false);
            var docRef = GlobalData.database.Collection("Counters").Document("TicketterCounts");
            await docRef.UpdateAsync("BuildEntry", FieldValue.Increment(1));
            var docSnap = await docRef.GetSnapshotAsync();
            int count = docSnap.GetValue<int>("BuildEntry");
            await ticket.ModifyAsync(prop => prop.Name = $"build-entry-{count.ToString("D4")}").ConfigureAwait(false);
        }

        #endregion TicketOptions
    }
}