using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AquacraftBot.Services
{
    public class BotConfiguration
    {
        [JsonProperty("BotName")]
        public string BotName { set; get; }
        [JsonProperty("LogoURL")]
        public string LogoURL { set; get; }
        [JsonProperty("Token")]
        public string token { set; get; }
        [JsonProperty("Prefixes")]
        public string[] prefixes { set; get; }
        [JsonProperty("EnableModeration")]
        public bool EnableModeration { set; get; } = true;
        [JsonProperty("EnableSuggestions")]
        public bool EnableSuggestions { set; get; } = false;
        [JsonProperty("EnableFunCommands")]
        public bool EnableFunCommands { set; get; } = false; // by default will be false for all command modules
        [JsonProperty("EnableModCommands")]
        public bool EnableModCommands { set; get; } = false;
        [JsonProperty("EnableUtilCommands")]
        public bool EnableUtilCommands { set; get; } = false;
        [JsonProperty("EnableGiveaway")]
        public bool EnableGiveaway { set; get; } = false;
    }
}
