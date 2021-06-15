using AquacraftBot.Services;
using AquacraftBot.Services.BotServices;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static AquacraftBot.Services.BotServices.BotServices;

namespace AquacraftBot.Commands.FunCmds
{
    public class EightBallCmd : BaseCommandModule
    {
        #region 8Ball
        [Command("8ball"), Description("8ball game. Where many life decisions are made")]
        [Aliases("8b", "8", "ask")]
        [Cooldown(1, 3, CooldownBucketType.Channel)]
        [GroupName(Group.Fun)]
        public async Task EightBall(CommandContext ctx,[RemainingText] string question = "")
        {
            if (string.IsNullOrEmpty(question)) {await BotServices.SendEmbedAsync(ctx, "Invalid or Null Input", "Please input your question that you want to ask").ConfigureAwait(false); return;
            }

            var sb = new StringBuilder();
            var replies = new List<string>();

            replies.Add("yes");
            replies.Add("no");
            replies.Add("maybe");
            replies.Add("unclear");

            var embed = new DiscordEmbedBuilder()
                .WithTitle(":8ball: Welcome to 8-ball!")
                .WithColor(DiscordColor.Black)
                .WithTimestamp(DateTime.UtcNow)
                .WithFooter($"Requested by {ctx.User.Username}");

            int n = new Random().Next(0 ,replies.Count * 3);
            if (n > replies.Count-1)
            {
                n %= replies.Count;
            }

            var answer = replies[n];
            sb.AppendLine($"Question: **{question}**?");
            sb.AppendLine($"Your answer is: **{answer}**.");
            embed.Description = sb.ToString();
            await ctx.RespondAsync(embed).ConfigureAwait(false);            
        }
        #endregion

        #region rolldice
        [Command("diceroll")]
        [Aliases("dice", "roll", "rolldice", "die")]
        [Description("Roll a six-sided die")]
        [GroupName(Group.Fun)]
        public async Task RollDice(CommandContext ctx)
        {
            var random = new Random();            
            await ctx.RespondAsync($"{ctx.User.Username} rolled a die and got {Formatter.Bold(random.Next(1,7).ToString())}").ConfigureAwait(false);
        }
        #endregion

        #region choose
        [Command("choose"), Description("Gives you a somewhat random choice between the words you specify (separated by spaces)")]        
        [GroupName(Group.Fun)]
        public async Task Random(CommandContext ctx, [RemainingText, Description("The word choices arguments separated by spaces.")] string input)
        {
            Random rnd = new Random();
            var inputs = input.Split(" ");
            await ctx.Channel.SendMessageAsync($"I have chosen **{inputs[rnd.Next(0, inputs.Length)]}**.").ConfigureAwait(false);
        }
        #endregion
    }
}