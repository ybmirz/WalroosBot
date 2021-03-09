using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AquacraftBot.Services
{
    public class GlobalData
    {
        //Just a class for Global Interclass Data that we might need
        public static DateTime startTime;
        public static DiscordColor defaultColour = new DiscordColor("#1167b1");
        public static List<string> prefixes;
        public static string logoURL = "https://cdn.discordapp.com/embed/avatars/0.png";
        public static string botName;
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
