# FACEIT Finder Plugin for CounterStrikeSharp

A CounterStrikeSharp plugin that retrieves and displays FACEIT levels for players when they join the server.

WIP to display these ranks in the leaderboard, similar to cs2-ranks.

## Features

- Automatically detects players' SteamIDs when they join the server
- Fetches FACEIT level information from the FACEIT API
- Displays FACEIT level to the player and optionally to all players on the server
- Configurable settings for notifications and minimum level announcements

## Requirements

- CounterStrikeSharp
- FACEIT API key

## Installation

1. Make sure you have CounterStrikeSharp installed on your CS2 server
2. Download the latest release of the FACEIT Finder Plugin
3. Extract the contents to your `csgo/addons/counterstrikesharp/plugins` directory
4. Restart your server or load the plugin using the CounterStrikeSharp plugin manager

## Configuration

The plugin creates a default configuration file at `csgo/addons/counterstrikesharp/plugins/FaceitFinderPlugin/FaceitFinderPlugin.json` with the following options:

```json
{
  "FaceitApiKey": "keyhere",
  "NotifyAllPlayers": true,
  "MinimumLevelToAnnounce": 0
}
```

- `FaceitApiKey`: Your FACEIT API key 
- `NotifyAllPlayers`: Whether to announce a player's FACEIT level to all players (true/false)
- `MinimumLevelToAnnounce`: The minimum FACEIT level required to announce to all players (0-10)

## Usage

The plugin works automatically:

1. When a player joins the server, the plugin retrieves their SteamID
2. The plugin queries the FACEIT API to get the player's FACEIT level
3. The player's FACEIT level is displayed in chat
4. If the player doesn't have a FACEIT account, a message is displayed indicating this
5. If the player's FACEIT level is at or above the `MinimumLevelToAnnounce` setting and `NotifyAllPlayers` is true, all players will be notified of the player's FACEIT level

## Building from Source

1. Clone the repository
2. Make sure you have .NET 8.0 SDK installed
3. Run `dotnet build` to build the plugin
4. Copy the compiled DLL to your `csgo/addons/counterstrikesharp/plugins` directory
