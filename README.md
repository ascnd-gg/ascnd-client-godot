# Ascnd Godot Plugin

> [!CAUTION]
> This project is experimental and under active development. Expect bugs, breaking changes, and incomplete features. Please report issues via the [issue tracker](../../issues).

Official Godot addon for the [Ascnd](https://ascnd.gg) leaderboard API. Submit scores, fetch leaderboards, and track player rankings with Godot-native signals and node lifecycle integration.

## Features

- **Node-based API** - Add `AscndClient` as a node in your scene
- **Export properties** - Configure API key and settings in the Inspector
- **Signal-based async** - Receive results via Godot signals (thread-safe)
- **Automatic lifecycle** - Client is disposed when the node exits the tree
- **Cross-language support** - Works with both C# and GDScript

## Requirements

- Godot 4.3+ with .NET support
- .NET 8.0 SDK

## Installation

### Option 1: Copy the addon folder (Recommended)

1. Download or clone this repository
2. Copy the `addons/ascnd/` folder to your project's `addons/` directory
3. Add the NuGet package reference to your project's `.csproj`:
   ```xml
   <PackageReference Include="Ascnd.Client" Version="1.0.0" />
   ```
4. Build your project (`dotnet build` or click **Build** in Godot's MSBuild panel)
5. Enable the plugin in **Project > Project Settings > Plugins**

### Option 2: Git submodule

```bash
# Add as submodule in your project root
git submodule add https://github.com/ascnd-gg/ascnd-client-godot.git external/ascnd-client-godot

# Copy the addon to your addons folder
cp -r external/ascnd-client-godot/addons/ascnd addons/
```

Then add the NuGet package reference and enable the plugin as described above.

## Quick Start

### 1. Add the AscndClient node

After enabling the plugin, add an `AscndClient` node to your scene. You can find it in the **Create New Node** dialog by searching for "AscndClient".

### 2. Configure in the Inspector

Set your API key in the Inspector panel:

| Property | Default | Description |
|----------|---------|-------------|
| **Api Key** | `""` | Your Ascnd API key ([get one here](https://dashboard.ascnd.gg)) |
| **Base Url** | `"https://api.ascnd.gg"` | API endpoint (default is fine for production) |
| **Timeout Seconds** | `30` | Request timeout in seconds |

### 3. Connect signals and call methods

```csharp
using Godot;
using Ascnd.Godot;

public partial class GameManager : Node
{
    private AscndClient _ascnd = null!;

    public override void _Ready()
    {
        _ascnd = GetNode<AscndClient>("AscndClient");

        // Verify API key is configured
        if (string.IsNullOrEmpty(_ascnd.ApiKey))
        {
            GD.PushWarning("AscndClient: API key not configured!");
            return;
        }

        // Connect signals
        _ascnd.ScoreSubmitted += OnScoreSubmitted;
        _ascnd.LeaderboardReceived += OnLeaderboardReceived;
        _ascnd.PlayerRankReceived += OnPlayerRankReceived;
        _ascnd.RequestFailed += OnRequestFailed;
    }

    public void SubmitPlayerScore(long score)
    {
        _ascnd.SubmitScore("my-leaderboard", "player-123", score);
    }

    public void FetchLeaderboard()
    {
        _ascnd.GetLeaderboard("my-leaderboard", limit: 10);
    }

    private void OnScoreSubmitted(string scoreId, int rank, bool isNewBest)
    {
        if (isNewBest)
        {
            GD.Print($"New personal best! Rank: #{rank}");
        }
        else
        {
            GD.Print($"Score submitted. Rank: #{rank}");
        }
    }

    private void OnLeaderboardReceived(
        Godot.Collections.Array entries,
        int totalEntries,
        bool hasMore)
    {
        GD.Print($"Leaderboard ({totalEntries} total entries):");
        foreach (Godot.Collections.Dictionary entry in entries)
        {
            GD.Print($"  #{entry["rank"]}: {entry["playerId"]} - {entry["score"]}");
        }

        if (hasMore)
        {
            GD.Print("  (more entries available)");
        }
    }

    private void OnPlayerRankReceived(int rank, long score, string percentile)
    {
        GD.Print($"Your rank: #{rank} (top {percentile})");
    }

    private void OnRequestFailed(string operation, string error)
    {
        GD.PrintErr($"API error in {operation}: {error}");
        // Handle error - show UI message, retry, etc.
    }
}
```

## API Reference

### Export Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ApiKey` | `string` | `""` | Your Ascnd API key |
| `BaseUrl` | `string` | `"https://api.ascnd.gg"` | API endpoint URL |
| `TimeoutSeconds` | `int` | `30` | Request timeout in seconds |

### Methods

#### SubmitScore(leaderboardId, playerId, score, metadata?, idempotencyKey?)

Submit a score to a leaderboard. Emits `ScoreSubmitted` on success or `RequestFailed` on error.

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `leaderboardId` | `string` | Yes | The leaderboard identifier |
| `playerId` | `string` | Yes | The player's unique identifier |
| `score` | `long` | Yes | The score value |
| `metadata` | `string` | No | Optional JSON metadata |
| `idempotencyKey` | `string` | No | Key to prevent duplicate submissions |

```csharp
// Basic submission
_ascnd.SubmitScore("weekly-highscores", "player-123", 42500);

// With metadata (JSON string)
_ascnd.SubmitScore("weekly-highscores", "player-123", 42500,
    metadata: "{\"character\": \"warrior\", \"level\": 15}");

// With idempotency key to prevent duplicates
_ascnd.SubmitScore("weekly-highscores", "player-123", 42500,
    idempotencyKey: "match-abc-final-score");
```

#### GetLeaderboard(leaderboardId, limit?, cursor?, aroundRank?, period?, viewSlug?)

Fetch leaderboard entries. Emits `LeaderboardReceived` on success or `RequestFailed` on error.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `leaderboardId` | `string` | (required) | The leaderboard identifier |
| `limit` | `int` | `10` | Entries to fetch (max: 100) |
| `cursor` | `string` | `""` | Cursor for keyset pagination (from `nextCursor` in response) |
| `aroundRank` | `int` | `0` | Jump to entries around a specific rank |
| `period` | `string` | `""` | Time period filter |
| `viewSlug` | `string` | `""` | Metadata view filter |

**Period values:**
- `""` (empty) - All-time leaderboard
- `"current"` - Current period (day/week/month based on leaderboard config)
- `"previous"` - Previous period
- ISO 8601 timestamp - Specific point in time

```csharp
// Get top 10 (default)
_ascnd.GetLeaderboard("weekly-highscores");

// Get top 25
_ascnd.GetLeaderboard("weekly-highscores", limit: 25);

// Cursor pagination for next page (use nextCursor from LeaderboardReceived signal)
_ascnd.GetLeaderboard("weekly-highscores", limit: 25, cursor: nextCursor);

// Jump to rank 500
_ascnd.GetLeaderboard("weekly-highscores", limit: 25, aroundRank: 500);

// Current period only
_ascnd.GetLeaderboard("weekly-highscores", period: "current");

// Filter by metadata view
_ascnd.GetLeaderboard("weekly-highscores", viewSlug: "platform-pc");
```

#### GetPlayerRank(leaderboardId, playerId, period?, viewSlug?)

Get a specific player's rank and percentile. Emits `PlayerRankReceived` on success or `RequestFailed` on error.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `leaderboardId` | `string` | (required) | The leaderboard identifier |
| `playerId` | `string` | (required) | The player's unique identifier |
| `period` | `string` | `""` | Time period filter |
| `viewSlug` | `string` | `""` | Metadata view filter |

```csharp
// Get player's all-time rank
_ascnd.GetPlayerRank("weekly-highscores", "player-123");

// Get player's rank for current period
_ascnd.GetPlayerRank("weekly-highscores", "player-123", period: "current");
```

#### Reinitialize()

Reinitialize the client with current export values. Call this if you change `ApiKey` or `BaseUrl` at runtime.

```csharp
_ascnd.ApiKey = "new-api-key";
_ascnd.Reinitialize();
```

### Signals

#### ScoreSubmitted(scoreId: string, rank: int, isNewBest: bool)

Emitted when a score is successfully submitted.

| Parameter | Type | Description |
|-----------|------|-------------|
| `scoreId` | `string` | Unique identifier for the submitted score |
| `rank` | `int` | Player's new rank on the leaderboard |
| `isNewBest` | `bool` | Whether this is the player's new personal best |

#### LeaderboardReceived(entries: Array, totalEntries: int, hasMore: bool)

Emitted when leaderboard data is received.

| Parameter | Type | Description |
|-----------|------|-------------|
| `entries` | `Array` | Array of Dictionary entries (see below) |
| `totalEntries` | `int` | Total number of entries on the leaderboard |
| `hasMore` | `bool` | Whether more entries exist beyond the current page |

Each entry Dictionary contains:
- `rank` (int) - Position on leaderboard
- `playerId` (string) - Player identifier
- `score` (long) - Score value
- `submittedAt` (string) - ISO 8601 timestamp
- `nextCursor` (string, optional) - Cursor for fetching the next page (present when `hasMore` is true)
- `bracket` (Dictionary, optional) - Contains `id`, `name`, `color`

#### PlayerRankReceived(rank: int, score: long, percentile: string)

Emitted when a player's rank is received.

| Parameter | Type | Description |
|-----------|------|-------------|
| `rank` | `int` | Player's rank position |
| `score` | `long` | Player's score |
| `percentile` | `string` | Percentile ranking (e.g., "5.2%") |

#### RequestFailed(operation: string, error: string)

Emitted when any API request fails.

| Parameter | Type | Description |
|-----------|------|-------------|
| `operation` | `string` | Method name that failed (`"SubmitScore"`, `"GetLeaderboard"`, `"GetPlayerRank"`) |
| `error` | `string` | Error message describing the failure |

## GDScript Usage

The addon works from GDScript. Note that signal names use `snake_case` in GDScript while method names remain `PascalCase`.

```gdscript
extends Node

@onready var ascnd: AscndClient = $AscndClient
var current_cursor: String = ""

func _ready() -> void:
    # Connect signals (use snake_case names)
    ascnd.score_submitted.connect(_on_score_submitted)
    ascnd.leaderboard_received.connect(_on_leaderboard_received)
    ascnd.player_rank_received.connect(_on_player_rank_received)
    ascnd.request_failed.connect(_on_request_failed)

    # Check if configured
    if ascnd.ApiKey.is_empty():
        push_warning("AscndClient: API key not set!")

func submit_score(score: int) -> void:
    ascnd.SubmitScore("my-leaderboard", "player-123", score)

func fetch_leaderboard() -> void:
    ascnd.GetLeaderboard("my-leaderboard", 10)  # top 10

func fetch_next_page() -> void:
    # Use cursor from previous response for pagination
    ascnd.GetLeaderboard("my-leaderboard", 10, current_cursor)

func jump_to_rank(target_rank: int) -> void:
    # Jump to entries around a specific rank
    ascnd.GetLeaderboard("my-leaderboard", 10, "", target_rank)

func get_my_rank() -> void:
    ascnd.GetPlayerRank("my-leaderboard", "player-123")

func _on_score_submitted(score_id: String, rank: int, is_new_best: bool) -> void:
    if is_new_best:
        print("New personal best! Rank: #%d" % rank)
    else:
        print("Score submitted. Rank: #%d" % rank)

func _on_leaderboard_received(entries: Array, total: int, has_more: bool) -> void:
    print("Leaderboard (%d total):" % total)
    for entry in entries:
        print("  #%d: %s - %d" % [entry.rank, entry.playerId, entry.score])
        # Store cursor for pagination
        if entry.has("nextCursor"):
            current_cursor = entry.nextCursor

func _on_player_rank_received(rank: int, score: int, percentile: String) -> void:
    print("Your rank: #%d (top %s)" % [rank, percentile])

func _on_request_failed(operation: String, error: String) -> void:
    push_error("API error in %s: %s" % [operation, error])
```

## Troubleshooting

### "ApiKey is not configured" error

The `ApiKey` export property is empty. Set it in the Inspector or via code before making API calls:
- **Inspector**: Select the AscndClient node and enter your API key
- **Code**: `_ascnd.ApiKey = "your-api-key"; _ascnd.Reinitialize();`

### Plugin not appearing in Project Settings

1. Ensure you've built the project (`dotnet build` or Godot's Build button)
2. Check that `addons/ascnd/plugin.cfg` exists
3. Verify your `.csproj` includes the `Ascnd.Client` package reference
4. Restart Godot after building

### Signals not being received

All signals are emitted via `CallDeferred` for thread safety. Ensure:
1. You're connecting signals in `_Ready()` or earlier
2. The AscndClient node is in the scene tree when making requests
3. Your signal handlers have the correct signature

### Build errors with "Ascnd.Client not found"

Add the NuGet package reference to your `.csproj`:
```xml
<ItemGroup>
  <PackageReference Include="Ascnd.Client" Version="1.0.0" />
</ItemGroup>
```

Then run `dotnet restore` or rebuild in Godot.

### Request timeouts

Increase the `TimeoutSeconds` property in the Inspector, or set it via code:
```csharp
_ascnd.TimeoutSeconds = 60; // 60 seconds
_ascnd.Reinitialize();
```

## Example Project

See the `examples/BasicUsage/` folder for a complete working example demonstrating score submission and leaderboard fetching.

## Links

- [Ascnd Website](https://ascnd.gg)
- [Dashboard](https://dashboard.ascnd.gg)
- [C# SDK Documentation](https://docs.ascnd.gg/sdks/csharp)
- [GitHub](https://github.com/ascnd-gg/ascnd-client-godot)

## License

MIT License - see [LICENSE](LICENSE) for details.
