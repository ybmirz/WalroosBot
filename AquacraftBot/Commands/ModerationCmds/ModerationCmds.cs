using AquacraftBot.Services;
using AquacraftBot.Services.BotServices;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using static AquacraftBot.Services.BotServices.BotServices;

namespace AquacraftBot.Commands.ModerationCmds
{
    public class ModerationCmds : BaseCommandModule
    {
        [Command("purge"), Description("Deletes a specified amount of messages in the channel, from any user.\nNeeds user to have Manage Messages Permission\n[Number specified not including the cmd message and has maximum of 100]")]
        [RequireBotPermissions(Permissions.ManageMessages)]
        [RequireUserPermissions(Permissions.ManageMessages)]
        [GroupName(Group.Moderation)]
        public async Task purge(CommandContext ctx, [Description("Number of messages to be deleted")] int amount)
        {
            if (amount < 1 || amount > 100) // conditional check
            {
                await BotServices.SendEmbedAsync(ctx, "Warning: False Amount Inputted", "Amount for `purge` has to be between (and equal to) 1 - 100. Please redo the command.", ResponseType.Warning).ConfigureAwait(false);
                return;
            }
            var msgsToDel = await ctx.Channel.GetMessagesAsync(amount + 1);
            await ctx.Channel.DeleteMessagesAsync(msgsToDel).ConfigureAwait(false);
        }

        [Command("ticketter"), Description("Enables and disables the ticketter function of the bot")]        
        [RequireRoles(RoleCheckMode.Any,"Manager [HR]", "Administrator [HR]")]
        [GroupName(Group.Moderation)]
        public async Task ticketTag(CommandContext ctx)
        {
            GlobalData.enableTicketter = !GlobalData.enableTicketter;
            var msg = GlobalData.enableTicketter ? "`Enabled`" : "`Disabled`";
            await ctx.RespondAsync($"Ticketter has been {msg}!").ConfigureAwait(false);            
        }

        [Command("rename"), Description("Renames the channel the command is executed in")]
        [RequirePermissions(Permissions.ManageChannels)]
        [GroupName(Group.Moderation)]
        public async Task channelRename(CommandContext ctx,[RemainingText] string newName)
        {
            await ctx.Channel.ModifyAsync(prop => prop.Name = newName).ConfigureAwait(false);
            await ctx.Message.DeleteAsync().ConfigureAwait(false);
        }

        [Command("closeticket"), Description("Sends a string of messages when wanting to close a ticket. Basically telling the user that they can close the ticket. Mod+")]
        [RequireUserPermissions(Permissions.ManageRoles)] // Basically only Mod+
        [GroupName(Group.Moderation)]
        public async Task ticketClose(CommandContext ctx)
        {
            //var cookie = DiscordEmoji.FromName(ctx.Client, ":uwucookie:");
            var lockEmote = DiscordEmoji.FromName(ctx.Client, ":lock:");
            var tickEmote = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");

            string msg = $"Thank you for creating a ticket. If there is no more questions/enquiries, you can go ahead close the ticket by reacting to the first message in the channel [{lockEmote}]," +
                $" continuing with the {tickEmote}.";
            await ctx.Message.DeleteAsync().ConfigureAwait(false);
            await ctx.Channel.SendMessageAsync(msg).ConfigureAwait(false);
        }        
    }

    [Group("announcer"), Description("Announce a recurring message when the bot is alive when it is set, with a set interval.")]
    [RequireUserPermissions(Permissions.ManageChannels)]
    [GroupName(Group.Moderation)]
    public class AnnouncerCmd : BaseCommandModule
    {
        [Command("set"), Description("Sets the announcement and the interval between announcement and the channel it will be set in")]
        [RequireUserPermissions(Permissions.ManageChannels)]
        public async Task set(CommandContext ctx, int intervalInMinutes, [RemainingText] string announcement)
        {
            var ms = TimeSpan.FromMinutes(intervalInMinutes).TotalMilliseconds;
            System.Timers.Timer announce = new System.Timers.Timer(ms);
            announce.Elapsed += (sender, e) => Announce_ElapsedAsync(sender, e, announcement, ctx.Channel);
            announce.Start();
            GlobalData.announcements.Add(announce);
            var msg = await ctx.RespondAsync($"Announcement set in channel! To remove, you can do `w!announcer remove {GlobalData.announcements.IndexOf(announce)}` <-- that number is the announcementIndex");
            await Task.Delay(3000);
            await msg.DeleteAsync().ConfigureAwait(false);
        }

        [Command("remove"), Description("A command to remove a recurring announcement")]
        [RequireUserPermissions(Permissions.ManageChannels)]
        public async Task remove(CommandContext ctx, int announcementIndex)
        {
            try
            {
                GlobalData.announcements[announcementIndex].Stop();
                GlobalData.announcements[announcementIndex].Dispose();
                await ctx.RespondAsync("Announcement has been removed!").ConfigureAwait(false);
            }
            catch 
            {
                await ctx.RespondAsync("It seems that announcementIndex does not exist in the list. Check again").ConfigureAwait(false);
            }
        }
        private async void Announce_ElapsedAsync(object sender, ElapsedEventArgs e, string message, DiscordChannel channel)
        {
            await channel.SendMessageAsync(message).ConfigureAwait(false);
        }
    }
}

