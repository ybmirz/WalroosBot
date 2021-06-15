using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AquacraftBot.Services.BotServices
{
    public class BotServices
    {
        //Fast SendEmbedAsync
        public static async Task SendEmbedAsync(CommandContext ctx, [Description("Title of Embed to be sent")] string title, [Description("Message to be sent on the Embed")] string desc, [Description("ResponseType to determine ErrorCode")] ResponseType type = ResponseType.Default)
        {
            var titleEmote = type switch
            {
                ResponseType.Warning => DiscordEmoji.FromName(ctx.Client, ":exclamation:"),
                ResponseType.Error => DiscordEmoji.FromName(ctx.Client, ":mag:"),
                ResponseType.Missing => DiscordEmoji.FromName(ctx.Client, ":no_entry:"),
                ResponseType.Default => DiscordEmoji.FromName(ctx.Client, ":bulb:"),
                _ => DiscordEmoji.FromName(ctx.Client, ":bulb:")
            };
            var ErrorColour = type switch
            {
                ResponseType.Default => GlobalData.defaultColour,
                ResponseType.Warning => new DiscordColor("#ffcc00"), //orange-ish warning colour
                ResponseType.Error => new DiscordColor("#cc3300"), //red error colour
                ResponseType.Missing => new DiscordColor("#999999"), //gray missing colour
                _ => GlobalData.defaultColour
            };

            var embed = new DiscordEmbedBuilder()
                .WithTitle(title + " " + titleEmote)
                .WithDescription(desc)
                .WithFooter($"{GlobalData.prefixes[0]}help for more Info", GlobalData.logoURL)
                .WithTimestamp(DateTime.Now)
                .WithColor(ErrorColour);

            var msg = await ctx.Channel.SendMessageAsync(embed: embed)
                .ConfigureAwait(false);
            await Task.Delay(12000); // 15 seconds before deleting the error message that pops up
            await msg.DeleteAsync().ConfigureAwait(false);
        }

        public static async Task SendWIPEmbedAsync(CommandContext ctx)
        {
            var sorryEmote = DiscordEmoji.FromName(ctx.Client, ":confounded:");

            var embed = new DiscordEmbedBuilder()
                .WithColor(GlobalData.defaultColour)
                .WithAuthor(GlobalData.botName, null, GlobalData.logoURL)
                .WithDescription($"Sorry! The command {GlobalData.prefixes[0]}{ctx.Command.QualifiedName} is still a work in progress {sorryEmote}");

            var msg = await ctx.RespondAsync(embed: embed).ConfigureAwait(false);
            await Task.Delay(12000); //12 seconds
            await msg.DeleteAsync().ConfigureAwait(false);
        }

        public static async Task SendDMEmbedAsync([Description("Member to be sent the embed to")]DiscordMember member, [Description("Title of Embed to be sent")] string title, [Description("Message to be sent in the embed")] string desc,[Description("Channel to send message in the case of unable to send dm")] DiscordChannel backup = null, [Description("ResponseType of the embed")] ResponseType type = ResponseType.Default)
        {
            var ErrorColour = type switch
            {
                ResponseType.Default => GlobalData.defaultColour,
                ResponseType.Warning => new DiscordColor("#ffcc00"), //orange-ish warning colour
                ResponseType.Error => new DiscordColor("#cc3300"), //red error colour
                ResponseType.Missing => new DiscordColor("#999999"), //gray missing colour
                _ => GlobalData.defaultColour
            };
            var embed = new DiscordEmbedBuilder()
               .WithTitle(title)
               .WithDescription(desc)
               .WithFooter($"Connected Guild: {member.Guild.Name}({member.Guild.Id})", GlobalData.logoURL)
               .WithTimestamp(DateTime.Now)
               .WithColor(ErrorColour);

            try
            { await member.SendMessageAsync(embed).ConfigureAwait(false); }
            catch (UnauthorizedException e) {
                if (backup != null)
                {
                    var msg = await backup.SendMessageAsync($"Oopsie, it seems that I am unable to send a DM to the user! It seems they have **disabled** DMs from server members. Please note the following message: `{desc}`.").ConfigureAwait(false);
                    await Task.Delay(10000); // 10 Seconds to wait for the message to dissappear
                    await msg.DeleteAsync().ConfigureAwait(false);
                }
                else
                {
                    Console.WriteLine("It seems that I was unable to send a suggestion submitter a DM message. The backup channel was as well unspecified. Moving on.");
                }
            }            
        }


        //creating command group module for Help Embed
        public class CmdGroup
        {
            public string GroupName;
            public List<Command> cmdList;
            public string groupEmoji;

            public CmdGroup(string name, string emoji)
            {
                this.GroupName = name;
                this.groupEmoji = emoji;
                this.cmdList = new List<Command>();
            }
        }
        //attribute for above class
        public class GroupNameAttribute : Attribute
        {
            public string name;
            public string emoji;

            public GroupNameAttribute(Group group)
            {
                this.name = group switch
                {
                    Group.Utilities => "Utilities",
                    Group.Moderation => "Moderation",
                    Group.Fun => "Fun",
                    Group.Suggestion => "Suggestion",
                    Group.Giveaway => "Giveaway"
                };
                this.emoji = group switch
                {
                    Group.Utilities => ":tools:",
                    Group.Moderation => ":wrench:",
                    Group.Fun => ":game_die:",
                    Group.Suggestion => ":memo:",
                    Group.Giveaway => ":confetti_ball:"
                };
            }
        }
    }
}
