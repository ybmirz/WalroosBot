using AquacraftBot.Services;
using AquacraftBot.Services.BotServices;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AquacraftBot.Services.BotServices.BotServices;

namespace AquacraftBot.Commands.FunCmds
{
    public class RPSCmd : BaseCommandModule
    {
        [Command("rps"), Description("Rock Paper Scissors Game. Fight against the SteebBot.")]
        [Cooldown(1, 3, CooldownBucketType.Channel)]
        [GroupName(Group.Fun)]
        public async Task rps(CommandContext ctx, [RemainingText] string choice = null)
        {
            if (string.IsNullOrWhiteSpace(choice))
            { await BotServices.SendEmbedAsync(ctx, "Invalid or Null Input", "Please input your choice to fight with me :>").ConfigureAwait(false); return; }

            string[] choices = new string[3] { "rock", "paper", "scissors" };
            if (choices.Contains($"{choice.ToLower()}") == false)
            {
                var msg = await ctx.RespondAsync($"Sorry that input does not exist :< . Please retry again.").ConfigureAwait(false);
                await Task.Delay(2000);
                await msg.DeleteAsync().ConfigureAwait(false);
                return;
            }

            Random rnd = new Random();
            int n = rnd.Next(0, 2);
            //the resulting win
            string[] resultStr = new string[3] { $"**{ctx.User.Username}** has won"/* 0 */, "It's a **tie**" /* 1 */ , "Haha loser, I won" /* 2 */};
            //emded for the winning result
            var resultEmbed = new DiscordEmbedBuilder { Title = "RPS Results" };
            StringBuilder resultSb = new StringBuilder();

            resultSb.AppendLine($"{ctx.User.Username} chose: **{choice}**");            
            resultSb.AppendLine($"I whip out: **{choices[n]}**");            
            resultSb.AppendLine(resultStr[rpsFight(choice, choices[n])]);

            resultEmbed.Description = resultSb.ToString();


            switch (rpsFight(choice, choices[n]))
            {
                case 0:
                    resultEmbed.WithThumbnail(ctx.User.AvatarUrl);
                    resultEmbed.Color = GlobalData.defaultColour;
                    break;
                case 1:                     
                    resultEmbed.Color = DiscordColor.Cyan;
                    break;
                case 2:
                    resultEmbed.WithThumbnail(GlobalData.logoURL);
                    resultEmbed.Color = DiscordColor.DarkRed;
                    break;
            }

            await ctx.Channel.SendMessageAsync(embed: resultEmbed.Build()).ConfigureAwait(false);
        }

        //rps fucntion returns 0 [Player Won] 1[tie] 2 [Player lost]
        private int rpsFight(string userChoice, string compChoice) //param: usrChoice of type string 
        {
            userChoice = userChoice.ToUpper();
            compChoice = compChoice.ToUpper();

            if (userChoice == "ROCK" && compChoice == "SCISSORS")
            {
                return 0;
            }
            else if (userChoice == "ROCK" && compChoice == "PAPER")
            {
                return 2;
            }
            else if (userChoice == "PAPER" && compChoice == "ROCK")
            {
                return 0;
            }
            else if (userChoice == "PAPER" && compChoice == "SCISSORS")
            {
                return 2;
            }
            else if (userChoice == "SCISSORS" && compChoice == "ROCK")
            {
                return 2;
            }
            else if (userChoice == "SCISSORS" && compChoice == "PAPER")
            {
                return 0;
            }
            else
            {
                return 1;
            }

        }

    }
}