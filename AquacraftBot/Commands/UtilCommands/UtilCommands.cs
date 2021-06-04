using AquacraftBot.Services;
using AquacraftBot.Services.BotServices;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity.Extensions;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static AquacraftBot.Services.BotServices.BotServices;
using Group = AquacraftBot.Services.Group;

namespace AquacraftBot.Commands.UtilCommands
{
    public class UtilCommands : BaseCommandModule
    {
        [Command("ping"), Description("Checks your ping")]
        [Aliases("pong")]
        [GroupName(Services.Group.Utilities)]
        public async Task Ping(CommandContext ctx)
        {
            var ping = DateTime.Now - ctx.Message.CreationTimestamp;
            string desc = $"Latency is `{ping.Milliseconds}ms`\nAPI Latency is `{ctx.Client.Ping}ms`";


            var embed = new DiscordEmbedBuilder()
                .WithColor(GlobalData.defaultColour)
                .WithTimestamp(DateTime.Now)
                .WithTitle(":ping_pong: " + Formatter.Bold("Pong!"))
                .WithFooter($"Requested by {ctx.User.Username}")
                .WithDescription(desc);


            await ctx.Channel.SendMessageAsync(embed.Build()).ConfigureAwait(false);

        }

        [Command("say"), Description("Simple repetition command of what you want the bot to say, needs user to have Manage Messages permission.")]
        [RequireUserPermissions(Permissions.ManageMessages)]
        [GroupName(Services.Group.Utilities)]
        public async Task say(CommandContext ctx, [RemainingText][Description("The msg you want the bot to repeat")] string str)
        {
            if (str != null)
            {
                await ctx.TriggerTypingAsync();
                if (str.Contains("<") || str.Contains(">"))
                {
                    Regex rgx = new Regex(@"<(?<id>\d+)>"); // Gets a match into a group named `id` between the two placeholder
                    MatchCollection matches = rgx.Matches(str);
                    /*Console.WriteLine("{0} matches found in:\n   {1}",
                          matches.Count,
                          str);*/
                    foreach (Match match in matches)
                    {                        
                        ulong id = Convert.ToUInt64(match.Groups[1].Value);                        
                        if (ctx.Guild.Members.ContainsKey(id))
                        {
                            var member = await ctx.Guild.GetMemberAsync(id);
                            str = str.Replace($"<{id}>", member.Mention);
                            continue;
                        }
                        if (ctx.Guild.Roles.ContainsKey(id))
                        {
                            var role = ctx.Guild.GetRole(id);
                            str = str.Replace($"<{id}>", role.Mention);
                            continue;
                        }
                        await ctx.Message.DeleteAsync();
                        var msg = await ctx.RespondAsync("Sadly, the placeholder ID is not a user's or role's ID. Please check again.").ConfigureAwait(false);
                        await Task.Delay(3000);
                        await msg.DeleteAsync().ConfigureAwait(false);
                        return;
                    }
                }
                await ctx.Message.DeleteAsync();
                await ctx.Channel.SendMessageAsync(str).ConfigureAwait(false);
            }
            else
            {
                var botmsg = await ctx.Channel.SendMessageAsync("What should I have said? Sadly you didn't specify it.");
                Thread.Sleep(2000);
                await ctx.Channel.DeleteMessageAsync(botmsg);
                await ctx.Message.DeleteAsync();
            }
        }

        [Command("uptime"), Description("The uptime of the bot. How long it has been running and being a cute walroos")]
        [Cooldown(1, 5, CooldownBucketType.Channel)]
        [GroupName(Group.Utilities)]
        public async Task uptime(CommandContext ctx)
        {
            var timespan = DateTime.Now - GlobalData.startTime;
            string desc = $"**{timespan.Days}** days, **{timespan.Hours}** hours, **{timespan.Minutes}** minutes, and **{timespan.Seconds}** seconds.";
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Uptime 🚀")
                .WithDescription(desc)
                .WithColor(GlobalData.defaultColour)
                .WithFooter($"Requested by {ctx.User.Username}")
                .WithTimestamp(DateTime.Now);
            await ctx.RespondAsync(embed).ConfigureAwait(false);
        }

        [Command("whois"), Description("Returns back information on the user specified from the server. Such as, Joined and Created Timestamps, Roles and Permissions said user has in the channel")]
        [Cooldown(1, 5, CooldownBucketType.Channel)]
        [RequireBotPermissions(Permissions.Administrator)]
        [GroupName(Group.Utilities)]
        public async Task whois(CommandContext ctx, DiscordMember discordUser)
        {
            //create main output embed
            var output = new DiscordEmbedBuilder();
            output.WithAuthor(discordUser.Username + "#" + discordUser.Discriminator, null, discordUser.AvatarUrl)
                .WithColor(GlobalData.defaultColour)
                .WithThumbnail(discordUser.AvatarUrl)
                .WithDescription(discordUser.Mention);

            //adds the joined at timestamp and created at timestamp
            output.AddField("Joined At", discordUser.JoinedAt.ToString("f"), true);
            output.AddField("Created At", discordUser.CreationTimestamp.ToString("f"), true);

            //gets the roles of the user
            var roles = discordUser.Roles;
            int roleCount = 0;
            string roleMentions = "";
            foreach (var role in roles)
            {
                roleCount++;
                roleMentions += role.Mention;
            }
            output.AddField($"Roles[{roleCount}]", roleMentions);

            //get permissions *(needs alot of review here)
            var perms = discordUser.PermissionsIn(ctx.Channel);
            output.AddField("Permissions", perms.ToPermissionString());

            //add footer
            output.WithFooter($"ID: {discordUser.Id}")
                .WithTimestamp(DateTime.Now);

            await ctx.Channel.SendMessageAsync(output).ConfigureAwait(false);
        }


        [Command("info"), Description("Returns back information on the bot and the bot developer.")]
        [Cooldown(1, 5, CooldownBucketType.Channel)]
        [GroupName(Group.Utilities)]
        public async Task info(CommandContext ctx, string keyword)
        {
            var embed = new DiscordEmbedBuilder()
                .WithAuthor($"{GlobalData.botName} Information")
                .WithColor(GlobalData.defaultColour)
                .WithTimestamp(DateTime.Now)
                .WithDescription($"Here's a little bit of information! If you need help with commands, use `{GlobalData.prefixes[0]}help`");

            embed.AddField("Current Guild", Formatter.BlockCode($"<RequestorID: {ctx.User.Id}>\n<ChannelID: {ctx.Channel.Id}>\n<GuildID: {ctx.Guild.Id}>", "xml"));
            embed.AddField("Bot Information", Formatter.BlockCode($"<Bot-UserID: {ctx.Client.CurrentUser.Id}>\n<ShardID: {ctx.Client.ShardId}>\n" +
                $"<Ping: {ctx.Client.Ping} ms>\n<Uptime: {(DateTime.Now - GlobalData.startTime).TotalMinutes} minutes>\n<CreationTimestamp: {ctx.Client.CurrentUser.CreationTimestamp:D}>", "xml"));
            var dev = await ctx.Guild.GetMemberAsync(574558925224017920).ConfigureAwait(false);
            string desc = "Thank you for using my bot! This was a fun project to work on, please check out my github page for my portfolio. https://github.com/ybmirz";
            embed.AddField("Developer Information", Formatter.BlockCode($"<Username: {dev.Username}#{dev.Discriminator}>\n<UserID: {dev.Id}>\n<Note: {desc}>", "xml"));

            await ctx.RespondAsync(embed.Build()).ConfigureAwait(false);
        }

        [Command("serverinfo"), Description("Returns back information on the discord server")]
        [Cooldown(1, 5, CooldownBucketType.Channel)]
        [GroupName(Group.Utilities)]
        public async Task serverInfo(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
            .WithAuthor(ctx.Guild.Name, null, ctx.Guild.IconUrl)
            .WithFooter($"Requested by {ctx.User.Username}")
            .WithColor(GlobalData.defaultColour)
            .WithTimestamp(DateTime.Now);

            embed.AddField("ID", ctx.Guild.Id.ToString(), true);
            embed.AddField("Owner", ctx.Guild.Owner.Username + "#" + ctx.Guild.Owner.Discriminator, true);

            var channels = ctx.Guild.Channels.Values;
            int text = 0, voice = 0, news = 0;
            foreach (var channel in channels)
            {
                if (channel.Type == ChannelType.Text)
                    text++;
                if (channel.Type == ChannelType.Voice)
                    voice++;
                if (channel.Type == ChannelType.News)
                    news++;
            }

            embed.AddField("Channels", $"{text} Text | {voice} Voice | {news} News", true);
            embed.AddField("Members", $"{ctx.Guild.MemberCount} Members", true);
            embed.AddField("Voice Region", ctx.Guild.VoiceRegion.Name, true);
            embed.AddField("Verification Level", ctx.Guild.MfaLevel.ToString(), true);

            LocalDate created = LocalDate.FromDateTime(ctx.Guild.CreationTimestamp.UtcDateTime);
            LocalDate now = LocalDate.FromDateTime(DateTime.UtcNow);
            Period period = Period.Between(created, now, PeriodUnits.YearMonthDay);
            embed.AddField("server Created", $"{period.Years} years, {period.Months} months, {period.Days} days. ({ctx.Guild.CreationTimestamp.UtcDateTime:D})");

            var roles = ctx.Guild.Roles.Values;
            string rolementions = string.Join(",", roles.Select(Formatter.Mention));
            embed.AddField("Roles", rolementions);

            await ctx.RespondAsync(embed.Build()).ConfigureAwait(false);
        }

        [Command("firestarter"), Description("A once in 2 hours command to start up a dead chat by pinging the Firestarter role")]
        [GroupName(Group.Fun)]
        [Cooldown(1, 7200, CooldownBucketType.Guild)]
        public async Task firestarter(CommandContext ctx)
        {
            // Firestart role id: 844879909544656928
            var role = ctx.Guild.GetRole(844879909544656928);
            var mainChat = ctx.Guild.GetChannel(748028461599424537);
            await ctx.Message.DeleteAsync().ConfigureAwait(false);
            await mainChat.SendMessageAsync($"Wake up wake up people! {role.Mention} let's get this fire started!").ConfigureAwait(false);
        }
    }
}