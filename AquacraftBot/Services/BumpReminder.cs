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
using System.Timers;

namespace AquacraftBot.Services
{
    public class BumpReminder
    {
        public static async void RemindElapsed(object sender, ElapsedEventArgs e, MessageAcknowledgeEventArgs msg, Timer reminder)
        {
            var channel = msg.Channel;
            DiscordRole bumpRole = msg.Channel.Guild.GetRole(842305189791924224);
            var rnd = new Random();
            var index = rnd.Next(0, GlobalData.AquaEmotes.Count);
            string message = $"Heya {bumpRole.Mention}, it's time to bump for our server. Bump our server by typing `!d bump`! {GlobalData.AquaEmotes.ElementAt(index).Value}";
            await channel.SendMessageAsync(message).ConfigureAwait(false);
            reminder.Stop();
            reminder.Dispose();
        }

        public static async void BumpThanks(DiscordUser bumper, DiscordChannel bumpChannel)
        {
            await bumpChannel.SendMessageAsync($"Thenk you for bumping our server up the list! We'll remind you to bump in 2 hours. {bumper.Mention}").ConfigureAwait(false);
        }
    }
}