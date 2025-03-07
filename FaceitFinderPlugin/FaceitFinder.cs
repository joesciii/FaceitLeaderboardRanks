using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;

namespace FaceitFinderPlugin
{
    [MinimumApiVersion(202)]
    public class FaceitFinderPlugin : BasePlugin, IPluginConfig<FaceitFinderConfig>
    {
        // Configuration
        public FaceitFinderConfig Config { get; set; } = new FaceitFinderConfig();
        
        // HttpClient for making API requests
        private static readonly HttpClient client = new HttpClient();

        public override string ModuleName => "FACEIT Finder Plugin";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "Your Name";
        public override string ModuleDescription => "Retrieves FACEIT level for players when they join the server";

        // Add this field to track which players we've already checked
        private HashSet<string> checkedPlayers = new HashSet<string>();

        public void OnConfigParsed(FaceitFinderConfig config)
        {
            Config = config;
        }

        public override void Load(bool hotReload)
        {
            Server.NextFrame(() => Console.WriteLine("FACEIT Finder Plugin loaded successfully!"));
            
            // Register event handlers
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        }

        private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            // Get the player controller from the event
            var player = @event.Userid;
            
            if (player == null || !player.IsValid)
            {
                Server.NextFrame(() => Console.WriteLine("Invalid player in OnPlayerConnectFull event"));
                return HookResult.Continue;
            }

            // Get the player's SteamID
            string steamId = player.SteamID.ToString();
            string playerName = player.PlayerName;
            
            // Log the SteamID with more details - using Server.NextFrame for all server operations
            Server.NextFrame(() =>
            {
                Console.WriteLine($"DEBUG: Player {playerName} connected with SteamID: {steamId}");
                Console.WriteLine($"DEBUG: Attempting to fetch FACEIT level for {playerName} with SteamID: {steamId}");
            });
            
            // Fetch FACEIT level asynchronously
            Task.Run(async () =>
            {
                try 
                {
                    Server.NextFrame(() => Console.WriteLine($"DEBUG: Starting API request for {playerName}"));
                    int? faceitLevel = await GetFaceitLevel(steamId, playerName);
                    Server.NextFrame(() => Console.WriteLine($"DEBUG: API request completed for {playerName}, result: {(faceitLevel.HasValue ? faceitLevel.Value.ToString() : "null")}"));
                    
                    if (!player.IsValid)
                        return;

                    if (faceitLevel.HasValue)
                    {
                        // Only announce if the level meets the minimum requirement
                        if (faceitLevel.Value >= Config.MinimumLevelToAnnounce)
                        {
                            // Notify the player and others about their FACEIT level
                            Server.NextFrame(() =>
                            {
                                if (player.IsValid)
                                {
                                    player.PrintToChat($" \u0004[FACEIT Finder]\u0001 Your FACEIT level is: {faceitLevel.Value}");
                                    Console.WriteLine($"[FACEIT Finder] {playerName}'s FACEIT level is: {faceitLevel.Value}");
                                    
                                    // Optionally notify all players
                                    if (Config.NotifyAllPlayers)
                                    {
                                        Server.PrintToChatAll($" \u0004[FACEIT Finder]\u0001 Player {playerName} has FACEIT level: {faceitLevel.Value}");
                                    }
                                }
                            });
                        }
                        else
                        {
                            // Just notify the player if their level is below the minimum
                            Server.NextFrame(() =>
                            {
                                if (player.IsValid)
                                {
                                    player.PrintToChat($" \u0004[FACEIT Finder]\u0001 Your FACEIT level is: {faceitLevel.Value}");
                                    Console.WriteLine($"[FACEIT Finder] {playerName}'s FACEIT level is: {faceitLevel.Value}");
                                }
                            });
                        }
                    }
                    else
                    {
                        // Notify if no FACEIT account was found
                        Server.NextFrame(() =>
                        {
                            if (player.IsValid)
                            {
                                player.PrintToChat($" \u0004[FACEIT Finder]\u0001 No FACEIT account found for your SteamID");
                                Console.WriteLine($"[FACEIT Finder] No FACEIT account found for {playerName} (SteamID: {steamId})");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Server.NextFrame(() => Console.WriteLine($"ERROR: {ex.Message}"));
                }
            });
            
            return HookResult.Continue;
        }

        private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            
            if (player == null || !player.IsValid)
                return HookResult.Continue;
            
            string steamId = player.SteamID.ToString();
            string playerName = player.PlayerName;
            
            // Only check once per map for this player
            if (!checkedPlayers.Contains(steamId))
            {
                checkedPlayers.Add(steamId);
                
                Server.NextFrame(() => Console.WriteLine($"DEBUG: Player {playerName} spawned with SteamID: {steamId}"));
                
                Task.Run(async () =>
                {
                    try
                    {
                        int? faceitLevel = await GetFaceitLevel(steamId, playerName);
                        
                        Server.NextFrame(() =>
                        {
                            if (!player.IsValid)
                                return;

                            if (faceitLevel.HasValue)
                            {
                                player.PrintToChat($" \u0004[FACEIT Finder]\u0001 Your FACEIT level is: {faceitLevel.Value}");
                                Console.WriteLine($"[FACEIT Finder] {playerName}'s FACEIT level is: {faceitLevel.Value}");
                                
                                if (Config.NotifyAllPlayers && faceitLevel.Value >= Config.MinimumLevelToAnnounce)
                                {
                                    Server.PrintToChatAll($" \u0004[FACEIT Finder]\u0001 Player {playerName} has FACEIT level: {faceitLevel.Value}");
                                }
                            }
                            else
                            {
                                player.PrintToChat($" \u0004[FACEIT Finder]\u0001 No FACEIT account found for your SteamID");
                                Console.WriteLine($"[FACEIT Finder] No FACEIT account found for {playerName} (SteamID: {steamId})");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Server.NextFrame(() => Console.WriteLine($"ERROR: {ex.Message}"));
                    }
                });
            }
            
            return HookResult.Continue;
        }

        public async Task<int?> GetFaceitLevel(string steamId, string playerName)
        {
            try
            {
                Server.NextFrame(() => Console.WriteLine($"DEBUG: Making FACEIT API request for SteamID: {steamId}"));
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Config.FaceitApiKey}");

                // Step 1: Get FACEIT ID using game_player_id instead of user_id
                string faceitUrl = $"https://open.faceit.com/data/v4/players?game=cs2&game_player_id={steamId}";
                Server.NextFrame(() => Console.WriteLine($"DEBUG: Request URL: {faceitUrl}"));
                var response = await client.GetAsync(faceitUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Server.NextFrame(() =>
                    {
                        Console.WriteLine($"DEBUG: Error fetching FACEIT ID: {response.StatusCode}");
                        Console.WriteLine($"DEBUG: Response content: {errorContent}");

                        // Try alternative endpoint if first one fails
                        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            Console.WriteLine($"DEBUG: Trying alternative endpoint...");
                        }
                    });

                    // If first attempt fails, try the alternative endpoint
                    faceitUrl = $"https://open.faceit.com/data/v4/players?game=cs2&game_player_id={steamId}&game_player_name={playerName}";
                    Server.NextFrame(() => Console.WriteLine($"DEBUG: Trying alternative URL: {faceitUrl}"));
                    response = await client.GetAsync(faceitUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        errorContent = await response.Content.ReadAsStringAsync();
                        Server.NextFrame(() =>
                        {
                            Console.WriteLine($"DEBUG: Error with alternative endpoint: {response.StatusCode}");
                            Console.WriteLine($"DEBUG: Response content: {errorContent}");
                        });
                        return null;
                    }
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                Server.NextFrame(() => Console.WriteLine($"DEBUG: Response body: {responseBody}"));
                var json = JObject.Parse(responseBody);
                string faceitId = json["player_id"]?.ToString();

                if (string.IsNullOrEmpty(faceitId))
                {
                    Server.NextFrame(() => Console.WriteLine("DEBUG: FACEIT ID not found in response"));
                    return null;
                }

                Server.NextFrame(() => Console.WriteLine($"DEBUG: Found FACEIT ID: {faceitId}"));

                // Step 2: Get FACEIT Level
                string playerUrl = $"https://open.faceit.com/data/v4/players/{faceitId}";
                Server.NextFrame(() => Console.WriteLine($"DEBUG: Player URL: {playerUrl}"));
                var playerResponse = await client.GetAsync(playerUrl);

                if (!playerResponse.IsSuccessStatusCode)
                {
                    var errorContent = await playerResponse.Content.ReadAsStringAsync();
                    Server.NextFrame(() =>
                    {
                        Console.WriteLine($"DEBUG: Error fetching player data: {playerResponse.StatusCode}");
                        Console.WriteLine($"DEBUG: Response content: {errorContent}");
                    });
                    return null;
                }

                var playerResponseBody = await playerResponse.Content.ReadAsStringAsync();
                Server.NextFrame(() => Console.WriteLine($"DEBUG: Player response body: {playerResponseBody}"));
                var playerJson = JObject.Parse(playerResponseBody);
                int? faceitLevel = playerJson["games"]?["cs2"]?["skill_level"]?.ToObject<int>();

                Server.NextFrame(() => Console.WriteLine($"DEBUG: FACEIT level found: {faceitLevel}"));
                return faceitLevel;
            }
            catch (Exception ex)
            {
                Server.NextFrame(() =>
                {
                    Console.WriteLine($"DEBUG: Error in GetFaceitLevel: {ex.Message}");
                    Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                });
                return null;
            }
        }
    }
}
