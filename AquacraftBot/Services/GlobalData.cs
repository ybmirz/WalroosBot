using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace AquacraftBot.Services
{
    public class GlobalData
    {
        //Just a class for Global Interclass Data that we might need
        public static DateTime startTime;
        public static DiscordColor defaultColour = new DiscordColor("#fd58d1");
        public static List<string> prefixes;
        public static string logoURL = "https://cdn.discordapp.com/embed/avatars/0.png";
        public static string botName;
        public static FirestoreDb database;
        public static bool enableTicketter = false;
        public static IReadOnlyDictionary<ulong, DiscordEmoji> AquaEmotes = new Dictionary<ulong, DiscordEmoji>();
        public static bool enableBumpReminder = false;
        public static List<DiscordMessage> ticketMsgs = new List<DiscordMessage>();
        public static List<Timer> announcements = new List<Timer>();
    }

    public enum ResponseType
    {
        Default,
        Warning,
        Missing,
        Error
    }

    public enum Group// group enums to make attributing prettier
    {
        Fun,
        Moderation,
        Utilities,
        Suggestion
    }

}
