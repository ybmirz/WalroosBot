using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace AquacraftBot.Services.SuggestionServices
{
    public class SuggestionService // A global class for Suggestion to look into after starting up the bot
    {
        public static ulong SuggestionChannel;
        public static ulong DecisionChannel;
        public static int VotingThreshold;
        public static bool DMonDecision;
        public static bool RefreshVoteOnEdit;
    }
}