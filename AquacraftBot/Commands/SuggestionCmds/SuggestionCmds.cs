using AquacraftBot.Services;
using AquacraftBot.Services.BotServices;
using AquacraftBot.Services.SuggestionServices;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AquacraftBot.Services.BotServices.BotServices;

namespace AquacraftBot.Commands.SuggestionCmds
{
    public class SuggestionCmds : BaseCommandModule
    {
        #region SuggestionMaking
        [Command("suggest"), Description("Creates a suggestion and output into the suggestions channel")]
        [GroupName(Group.Suggestion)]
        public async Task Suggest(CommandContext ctx,[RemainingText] [Description("Content of the Suggestion")] string content)
        {
            //variables to be used when creating a suggestion
            DiscordUser submitter = ctx.User;
            string sID = Guid.NewGuid().ToString().Substring(0, 8);
            var sEmbed = new DiscordEmbedBuilder() // default suggestion embed
                .WithColor(GlobalData.defaultColour)
                .WithTimestamp(DateTime.Now)
                .WithFooter("sID: " + sID)
                .WithThumbnail(submitter.AvatarUrl);

            sEmbed.AddField(Formatter.Bold("Submitter:"), submitter.Username + "#" + submitter.Discriminator);
            sEmbed.AddField(Formatter.Bold("Suggestion:"), content);

            //deletes the commands msg
            await ctx.Message.DeleteAsync().ConfigureAwait(false);
            //send message to channel set in suggestion service as "suggestion channel"
            var suggestionChannel = ctx.Guild.GetChannel(SuggestionService.SuggestionChannel);
            var sMsg = await suggestionChannel.SendMessageAsync(sEmbed.Build()).ConfigureAwait(false);

            //create a suggestion model with the above informations
            SuggestionModel suggestion = new SuggestionModel()
            {
                sID = sID,
                SubmitterID = submitter.Id,
                MessageID = sMsg.Id,
                Content = content
            };
            
            DocumentReference docRef = GlobalData.database.Collection("Suggestions").Document(sID); // creates a new document with the sID as its ID
            await docRef.SetAsync(suggestion); //sets the suggestion in the document

            //react to the msg so we can upvote and downvote
            var upvoteEmote = DiscordEmoji.FromName(ctx.Client, ":arrow_up:");
            var downvoteEmote = DiscordEmoji.FromName(ctx.Client, ":arrow_down:");

            await sMsg.CreateReactionAsync(upvoteEmote).ConfigureAwait(false);
            await sMsg.CreateReactionAsync(downvoteEmote).ConfigureAwait(false);

            // Sending the user a message saying suggestion has been made. If DMonDecision is on, if not it just logs to console.
            Console.WriteLine($"Suggestion #{sID} has been added, from {ctx.Guild.Name}({ctx.Guild.Id}) by {ctx.User.Username}#{ctx.User.Discriminator}({ctx.User.Id})");
            if (SuggestionService.DMonDecision)
            {                
                await BotServices.SendDMEmbedAsync(ctx.Member, $"Suggestion #{sID} has been added!", $"Your suggestion has been posted in {suggestionChannel.Mention}, with sID: {sID}. Make sure to {upvoteEmote} Upvote your suggestion as well. Please wait for staff decision on your suggestion.", ctx.Channel).ConfigureAwait(false);
            }
            
        }
        #endregion SuggestionMaking

        #region SuggestionDecision
        [Command("approve"), Description("Approves a suggestion. If reason is blank, then the reason would be \"No reason given.\". Must be done in channel with the suggestion and by user with Manage Channels Permission")]
        [GroupName(Group.Suggestion)]
        [RequireUserPermissions(Permissions.ManageChannels)]
        public async Task Approve(CommandContext ctx, [Description("sID of the Suggestion to decide on")] string sID, [RemainingText][Description("Reason on the decision")] string reason = "No reason given.")
        {
            //getting back data from firestore
            DocumentReference suggestionRef = GlobalData.database.Collection("Suggestions").Document(sID);
            DocumentSnapshot suggestionSnap = await suggestionRef.GetSnapshotAsync();

            if (suggestionSnap.Exists) // basically means that the document/suggestion exists
            {
                SuggestionModel suggestion = suggestionSnap.ConvertTo<SuggestionModel>();
                DiscordUser submitter = await ctx.Client.GetUserAsync(suggestion.SubmitterID).ConfigureAwait(false);
                DiscordMessage suggestionMsg = await ctx.Channel.GetMessageAsync(suggestion.MessageID).ConfigureAwait(false);
                DiscordUser admin = ctx.User;
                DiscordColor approveColor = DiscordColor.SpringGreen;

                var embed = decisionEmbed(suggestion, admin, submitter, suggestionMsg, reason, DecisionType.Approve);
                embed.WithColor(approveColor);

                //deletes the cmd msg
                await ctx.Message.DeleteAsync().ConfigureAwait(false);
                //sends the embed to the decision channel
                var decisionChannel = ctx.Guild.GetChannel(SuggestionService.DecisionChannel);
                DiscordMember member = null;
                try {
                    member = await ctx.Guild.GetMemberAsync(submitter.Id);
                }
                catch { }
                var msg = await decisionChannel.SendMessageAsync(embed.Build()).ConfigureAwait(false);

                if (SuggestionService.DMonDecision && member != null)
                    await BotServices.SendDMEmbedAsync(member, $"Suggestion #{sID} has been approved!", $"Your suggestion with sID:`{sID}` has been approved in {decisionChannel.Mention}. Thank you for the suggestion! Response: {msg.JumpLink}.", ctx.Channel, ResponseType.Default).ConfigureAwait(false);

                //deletes the suggestion msg and the suggestion doc on firestore
                await suggestionRef.DeleteAsync();
                await suggestionMsg.DeleteAsync(); 
            }
            else
            {
                await BotServices.SendEmbedAsync(ctx, "Suggestion Not Found", $"Suggestion with sID: {sID} was not found. If this suggestion does exist, please use `{GlobalData.prefixes[0]}info` to contact the developer.", ResponseType.Error).ConfigureAwait(false);
            }
        }

        [Command("deny"), Description("Denies a suggestion. If reason is blank, then the reason would be \"No reason given.\". Must be done in channel with the suggestion and by user with Manage Channels Permission")]
        [GroupName(Group.Suggestion)]
        [RequireUserPermissions(Permissions.ManageChannels)]
        public async Task deny(CommandContext ctx, [Description("sID of the Suggestion to decide on")] string sID, [RemainingText][Description("Reason on the decision")] string reason = "No reason given.")
        {
            //getting back data from firestore
            DocumentReference suggestionRef = GlobalData.database.Collection("Suggestions").Document(sID);
            DocumentSnapshot suggestionSnap = await suggestionRef.GetSnapshotAsync();

            if (suggestionSnap.Exists) // basically means that the document/suggestion exists
            {
                SuggestionModel suggestion = suggestionSnap.ConvertTo<SuggestionModel>();
                DiscordUser submitter = await ctx.Client.GetUserAsync(suggestion.SubmitterID).ConfigureAwait(false);
                DiscordMessage suggestionMsg = await ctx.Channel.GetMessageAsync(suggestion.MessageID).ConfigureAwait(false);
                DiscordUser admin = ctx.User;
                DiscordColor denyColor = DiscordColor.Red;

                var embed = decisionEmbed(suggestion, admin, submitter, suggestionMsg, reason, DecisionType.Denied);
                embed.WithColor(denyColor);

                //deletes the cmd msg
                await ctx.Message.DeleteAsync().ConfigureAwait(false);
                //sends the embed to the decision channel 
                var decisionChannel = ctx.Guild.GetChannel(SuggestionService.DecisionChannel);
                DiscordMember member = null;
                try
                {
                    member = await ctx.Guild.GetMemberAsync(submitter.Id);
                }
                catch { }
                var msg = await decisionChannel.SendMessageAsync(embed.Build()).ConfigureAwait(false);

                if (SuggestionService.DMonDecision && member != null)
                    await BotServices.SendDMEmbedAsync(member, $"Suggestion #{sID} has been denied!", $"Your suggestion with sID:`{sID}` has been denied in {decisionChannel.Mention}. Thank you for the suggestion! Response: {msg.JumpLink}.", ctx.Channel, ResponseType.Error).ConfigureAwait(false);

                //deletes the suggestion msg and the suggestion doc on firestore
                await suggestionRef.DeleteAsync();
                await suggestionMsg.DeleteAsync();
            }
            else
            {
                await BotServices.SendEmbedAsync(ctx, "Suggestion Not Found", $"Suggestion with sID: {sID} was not found. If this suggestion does exist, please use `{GlobalData.prefixes[0]}info` to contact the developer", ResponseType.Error).ConfigureAwait(false);
            }
        }

        [Command("implemented"), Description("States that a suggestion has been implemented. If reason is blank, then the reason would be \"Implemented.\". Must be done in channel with the suggestion and by user with Manage Channels Permission")]
        [GroupName(Group.Suggestion)]
        [RequireUserPermissions(Permissions.ManageChannels)]
        public async Task Implemented(CommandContext ctx, [Description("sID of the Suggestion to decide on")] string sID, [RemainingText][Description("Reason on the decision")] string reason = "Implemented.")
        {
            //getting back data from firestore
            DocumentReference suggestionRef = GlobalData.database.Collection("Suggestions").Document(sID);
            DocumentSnapshot suggestionSnap = await suggestionRef.GetSnapshotAsync();

            if (suggestionSnap.Exists) // basically means that the document/suggestion exists
            {
                SuggestionModel suggestion = suggestionSnap.ConvertTo<SuggestionModel>();
                DiscordUser submitter = await ctx.Client.GetUserAsync(suggestion.SubmitterID).ConfigureAwait(false);
                DiscordMessage suggestionMsg = await ctx.Channel.GetMessageAsync(suggestion.MessageID).ConfigureAwait(false);
                DiscordUser admin = ctx.User;
                DiscordColor implementedColor = DiscordColor.CornflowerBlue;

                var embed = decisionEmbed(suggestion, admin, submitter, suggestionMsg, reason, DecisionType.Implemented);
                embed.WithColor(implementedColor);

                //deletes the cmd msg
                await ctx.Message.DeleteAsync().ConfigureAwait(false);
                //sends the embed to the decision channel *(for now its ctx.channel)
                var decisionChannel = ctx.Guild.GetChannel(SuggestionService.DecisionChannel);
                DiscordMember member = null;
                try
                {
                    member = await ctx.Guild.GetMemberAsync(submitter.Id);
                }
                catch { }
                var msg = await decisionChannel.SendMessageAsync(embed.Build()).ConfigureAwait(false);

                if (SuggestionService.DMonDecision && member != null)
                    await BotServices.SendDMEmbedAsync(member, $"Suggestion #{sID} has been implemented!", $"Your suggestion with sID:`{sID}` has been implemented in {decisionChannel.Mention}. Thank you for the suggestion! Response: {msg.JumpLink}.", ctx.Channel, ResponseType.Default).ConfigureAwait(false);

                //deletes the suggestion msg and the suggestion doc on firestore
                await suggestionRef.DeleteAsync();
                await suggestionMsg.DeleteAsync();
            }
            else
            {
                await BotServices.SendEmbedAsync(ctx, "Suggestion Not Found", $"Suggestion with sID: {sID} was not found. If this suggestion does exist, please use `{GlobalData.prefixes[0]}info` to contact the developer.", ResponseType.Error).ConfigureAwait(false);
            }
        }

        [Command("considered"), Description("States that a suggestion is in consideration. If reason is blank, then the reason would be \"Considered.\". Must be done in channel with the suggestion and by user with Manage Channels Permission")]
        [GroupName(Group.Suggestion)]
        [RequireUserPermissions(Permissions.ManageChannels)]
        public async Task Considered(CommandContext ctx, [Description("sID of the Suggestion to decide on")] string sID, [RemainingText][Description("Reason on the decision")] string reason = "Considered.")
        {
            //getting back data from firestore
            DocumentReference suggestionRef = GlobalData.database.Collection("Suggestions").Document(sID);
            DocumentSnapshot suggestionSnap = await suggestionRef.GetSnapshotAsync();

            if (suggestionSnap.Exists) // basically means that the document/suggestion exists
            {
                SuggestionModel suggestion = suggestionSnap.ConvertTo<SuggestionModel>();
                DiscordUser submitter = await ctx.Client.GetUserAsync(suggestion.SubmitterID).ConfigureAwait(false);
                DiscordMessage suggestionMsg = await ctx.Channel.GetMessageAsync(suggestion.MessageID).ConfigureAwait(false);
                DiscordUser admin = ctx.User;

                var embed = decisionEmbed(suggestion, admin, submitter, suggestionMsg, reason, DecisionType.Considered);
                embed.WithColor(GlobalData.defaultColour);

                //deletes the cmd msg
                await ctx.Message.DeleteAsync().ConfigureAwait(false);
                //sends the embed to the decision channel
                var decisionChannel = ctx.Guild.GetChannel(SuggestionService.DecisionChannel);
                DiscordMember member = null;
                try
                {
                    member = await ctx.Guild.GetMemberAsync(submitter.Id);
                }
                catch { }
                var msg = await decisionChannel.SendMessageAsync(embed.Build()).ConfigureAwait(false);

                if (SuggestionService.DMonDecision && member != null)
                    await BotServices.SendDMEmbedAsync(member, $"Suggestion #{sID} is in consideration!", $"Your suggestion with sID:`{sID}` is in consideration in {decisionChannel.Mention}. Thank you for the suggestion! Response: {msg.JumpLink}.", ctx.Channel,ResponseType.Warning).ConfigureAwait(false);

                //deletes the suggestion msg and the suggestion doc on firestore
                await suggestionRef.DeleteAsync();
                await suggestionMsg.DeleteAsync();
            }
            else
            {
                await BotServices.SendEmbedAsync(ctx, "Suggestion Not Found", $"Suggestion with sID: {sID} was not found. If this suggestion does exist, please use `{GlobalData.prefixes[0]}info` to contact the developer", ResponseType.Error).ConfigureAwait(false);
            }
        }


        private DiscordEmbedBuilder decisionEmbed(SuggestionModel suggestion, DiscordUser admin, DiscordUser submitter, DiscordMessage suggestionMsg, string reason, DecisionType decisionType) 
        {
            //gets the reactions from the msg and write to a string enumerable
            var result = suggestionMsg.Reactions;
            var results = result.Select(x => $"{x.Emoji}: **{x.Count - 1}**");

            var decisionEmbed = new DiscordEmbedBuilder()
                .WithAuthor(submitter.Username, null, submitter.AvatarUrl)
                .WithTimestamp(DateTime.Now)
                .WithFooter("sID: " + suggestion.sID);

            //the fields of the embed
            decisionEmbed.AddField(Formatter.Bold("Results"), string.Join("\n", results));
            decisionEmbed.AddField(Formatter.Bold("Suggestion:"), suggestion.Content);
            decisionEmbed.AddField(Formatter.Bold("Submitter:"), submitter.Mention);

            var msg = decisionType switch
            {
                DecisionType.Approve => "Approved By:",
                DecisionType.Denied => "Denied By:",
                DecisionType.Implemented => "Implemented By:",
                DecisionType.Considered => " In Consideration By:"
            };

            decisionEmbed.AddField(Formatter.Bold(msg), admin.Mention);
            decisionEmbed.AddField(Formatter.Bold("Reason:"), reason);

            return decisionEmbed;
        }
        #endregion SuggestionDecision
 
        #region SuggestionInfo
        [Command("suggestions"), Description("Returns the suggestion configuration of the current bot.")]
        [GroupName(Group.Suggestion)]
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task SuggestInfo(CommandContext ctx)
        {
            var suggestionChannel = ctx.Guild.GetChannel(SuggestionService.SuggestionChannel);
            var decisionChannel = ctx.Guild.GetChannel(SuggestionService.DecisionChannel);

            string desc = "Suggestion channel: " + suggestionChannel.Mention +"\n" +
                "Decision channel: " + decisionChannel.Mention + "\n" +
                "Voting Threshold (Difference between upvotes and downvotes for the suggestion color to be sent to owner. NOT IMPLEMENTED): " + SuggestionService.VotingThreshold + "\n" +
                "Submitter DM Notification (Notify suggestion submitter on decision of suggestion): " + SuggestionService.DMonDecision;

            var embed = new DiscordEmbedBuilder()
                .WithColor(GlobalData.defaultColour)
                .WithTitle("Suggestion Configuration")
                .WithDescription(desc);
              
            await ctx.RespondAsync(embed.Build()).ConfigureAwait(false);    
        }
        #endregion SuggestionInfo
    }

    public enum DecisionType
    {
        Approve,
        Implemented,
        Denied,
        Considered
    }
}