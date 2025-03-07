using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace FaceitFinderPlugin
{
    public class FaceitFinderConfig : BasePluginConfig
    {
        [JsonPropertyName("FaceitApiKey")]
        public string FaceitApiKey { get; set; } = "apikeyhere";

        [JsonPropertyName("NotifyAllPlayers")]
        public bool NotifyAllPlayers { get; set; } = true;

        [JsonPropertyName("MinimumLevelToAnnounce")]
        public int MinimumLevelToAnnounce { get; set; } = 0;
    }
} 