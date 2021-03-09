using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static AquacraftBot.Services.BotServices.BotServices;

namespace AquacraftBot
{
    public class Commands : BaseCommandModule
    {
        [Command("ping"), Description("Checks your ping")]
        [Aliases("pong")]
        [GroupName(Services.Group.Utilities)]
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

        [Command("ping"), Description("Checks your ping")]
        [GroupName(Services.Group.Utilities)]
        public async Task Ping2(CommandContext ctx, [Description("Ping number")] int ping)
        {
            await ctx.TriggerTypingAsync();

            var botMsg = await ctx.Channel.SendMessageAsync(":chart_with_downwards_trend: Pinging ");

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

    [Group("memes")]
    [Description("Contains some memes. When invoked without subcommand, returns a random one.")]
    [GroupName(Services.Group.Fun)]
    [Aliases("copypasta")]
    public class ExampleExecutableGroup : BaseCommandModule
    {
        // commands in this group need to be executed as 
        // <prefix>memes [command] or <prefix>copypasta [command]

        // this command is a group command. It means it can be invoked by just typing <prefix>memes
        [GroupCommand]
        public async Task RandomMemeAsync(CommandContext ctx)
        {
            // let's give them a random meme
            var rnd = new Random();
            var nxt = rnd.Next(0, 2);

            switch (nxt)
            {
                case 0:
                    await Pepe(ctx);
                    return;

                case 1:
                    await NavySeal(ctx);
                    return;

                case 2:
                    await Kekistani(ctx);
                    return;
            }
        }

        [Command("pepe"), Aliases("feelsbadman"), Description("Feels bad, man.")]
        public async Task Pepe(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            // wrap it into an embed
            var embed = new DiscordEmbedBuilder
            {
                Title = "Pepe",
                ImageUrl = "http://i.imgur.com/44SoSqS.jpg"
            };
            await ctx.RespondAsync(embed);
        }

        [Command("navyseal"), Aliases("gorillawarfare"), Description("What the fuck did you just say to me?")]
        public async Task NavySeal(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync("What the fuck did you just fucking say about me, you little bitch? I’ll have you know I graduated top of my class in the Navy Seals, and I’ve been involved in numerous secret raids on Al-Quaeda, and I have over 300 confirmed kills. I am trained in gorilla warfare and I’m the top sniper in the entire US armed forces. You are nothing to me but just another target. I will wipe you the fuck out with precision the likes of which has never been seen before on this Earth, mark my fucking words. You think you can get away with saying that shit to me over the Internet? Think again, fucker. As we speak I am contacting my secret network of spies across the USA and your IP is being traced right now so you better prepare for the storm, maggot. The storm that wipes out the pathetic little thing you call your life. You’re fucking dead, kid. I can be anywhere, anytime, and I can kill you in over seven hundred ways, and that’s just with my bare hands. Not only am I extensively trained in unarmed combat, but I have access to the entire arsenal of the United States Marine Corps and I will use it to its full extent to wipe your miserable ass off the face of the continent, you little shit. If only you could have known what unholy retribution your little “clever” comment was about to bring down upon you, maybe you would have held your fucking tongue. But you couldn’t, you didn’t, and now you’re paying the price, you goddamn idiot. I will shit fury all over you and you will drown in it. You’re fucking dead, kiddo.");
        }

        [Command("kekistani"), Aliases("kek", "normies"), Description("I'm a proud ethnic Kekistani.")]
        public async Task Kekistani(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();
            await ctx.RespondAsync("I'm a proud ethnic Kekistani. For centuries my people bled under Normie oppression. But no more. We have suffered enough under your Social Media Tyranny. It is time to strike back. I hereby declare a meme jihad on all Normies. Normies, GET OUT! RRRÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ﻿");
        }
    }
}