using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AquacraftBot
{
    public class Commands : BaseCommandModule
    {
        [Command("ping"), Description("Checks your ping")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            var botMsg = await ctx.Channel.SendMessageAsync(":chart_with_downwards_trend: Pinging ");
            var ping = ctx.Client.Ping;

            //a rnadom msg to be sent
            var rnd = new Random();
            //var nxt = rnd.Next(0, 2);
            int nxt = 0;

            switch (nxt)
            {
                case 0:
                    var emoji2 = DiscordEmoji.FromName(ctx.Client, ":woah:");
                    await ctx.Channel.SendMessageAsync($"Interesting {ctx.Member.Mention} you have a internet connection ({ping} ms)").ConfigureAwait(false);
                    return;
                case 1:
                    var emoji = DiscordEmoji.FromName(ctx.Client, ":ok_hand:");
                    await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, your mom gay {emoji} ({ping} ms)").ConfigureAwait(false);
                    return;
                case 2:
                    var emoji3 = DiscordEmoji.FromName(ctx.Client, "::");
                    await ctx.Channel.SendMessageAsync($"hmm? you're approaching me? {ctx.Member.Mention} {emoji3} ({ping} ms)").ConfigureAwait(false);
                    return;

            }
        }
    }
}
