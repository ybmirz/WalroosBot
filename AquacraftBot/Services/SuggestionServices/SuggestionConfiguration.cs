using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AquacraftBot.Services.SuggestionServices
{
    public partial class SuggestionConfiguration
    {
        [JsonProperty("SuggestionChannel")]
        public ulong SuggestionChannel { get; set; }
        [JsonProperty("DecisionChannel")]
        public ulong DecisionChannel { get; set; }
        [JsonProperty("VotingThreshold")]
        public int Threshold { get; set; }
        [JsonProperty("DMonDecision")]
        public bool DMonDecision { get; set; }
        [JsonProperty("RefreshVoteOnEdit")]
        public bool RefreshVoteOnEdit { get; set; }
    }
    public partial class SuggestionConfiguration
    {
        public static SuggestionConfiguration GetConfig(string json) => JsonConvert.DeserializeObject<SuggestionConfiguration>(json);
    }
}
