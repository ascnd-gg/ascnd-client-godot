# Ascnd Godot Plugin

Official Godot addon for the [Ascnd](https://ascnd.gg) leaderboard API. Submit scores, fetch leaderboards, and track player rankings with Godot-native signals and node lifecycle integration.

## Features

- **Node-based API** - Add `AscndClient` as a node in your scene
- **Export properties** - Configure API key and settings in the Inspector
- **Signal-based async** - Receive results via Godot signals
- **Automatic lifecycle** - Client is disposed when the node exits the tree

## Installation

### Option 1: Copy the addon folder

1. Download or clone this repository
2. Copy the `addons/ascnd/` folder to your project's `addons/` directory
3. Add the NuGet package reference to your project's `.csproj`:
   ```xml
   <PackageReference Include="Ascnd.Client" Version="1.1.0" />
   ```
4. Build your project (dotnet build or Godot's Build button)
5. Enable the plugin in **Project > Project Settings > Plugins**

### Option 2: Use as a Git submodule

```bash
git submodule add https://github.com/ascnd-gg/ascnd-client-godot.git addons/ascnd-client-godot
```

Then copy `addons/ascnd-client-godot/addons/ascnd/` to your project's `addons/ascnd/`.

## Quick Start

### 1. Add the AscndClient node

Add an `AscndClient` node to your scene. You can find it in the "Create New Node" dialog after enabling the plugin.

### 2. Configure in the Inspector

Set your API key in the Inspector panel:
- **Api Key**: Your Ascnd API key (get one at https://dashboard.ascnd.gg)
- **Base Url**: API endpoint (default is fine for production)
- **Timeout Seconds**: Request timeout (default: 30)

### 3. Connect signals and call methods

```csharp
using Godot;

public partial class GameManager : Node
{
    private AscndClient _ascnd;

    public override void _Ready()
    {
        _ascnd = GetNode<AscndClient>("AscndClient");

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

    private void OnScoreSubmitted(string scoreId, long rank, bool isNewBest)
    {
        GD.Print($"Score submitted! Rank: #{rank}, New best: {isNewBest}");
    }

    private void OnLeaderboardReceived(
        Godot.Collections.Array entries,
        long totalEntries,
        bool hasMore)
    {
        foreach (Godot.Collections.Dictionary entry in entries)
        {
            GD.Print($"#{entry["rank"]}: {entry["playerId"]} - {entry["score"]}");
        }
    }

    private void OnPlayerRankReceived(long rank, long score, float percentile)
    {
        GD.Print($"Your rank: #{rank} (top {percentile:F1}%)");
    }

    private void OnRequestFailed(string operation, string error)
    {
        GD.PrintErr($"API error in {operation}: {error}");
    }
}
```

## API Reference

### Exports

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ApiKey` | string | "" | Your Ascnd API key |
| `BaseUrl` | string | "https://api.ascnd.gg" | API endpoint URL |
| `TimeoutSeconds` | int | 30 | Request timeout |

### Methods

#### SubmitScore(leaderboardId, playerId, score, metadata?, idempotencyKey?)

Submit a score to a leaderboard.

```csharp
_ascnd.SubmitScore("weekly-highscores", "player-123", 42500);

// With optional metadata (JSON string)
_ascnd.SubmitScore("weekly-highscores", "player-123", 42500,
    metadata: "{\"character\": \"warrior\", \"level\": 15}");

// With idempotency key to prevent duplicates
_ascnd.SubmitScore("weekly-highscores", "player-123", 42500,
    idempotencyKey: "game-session-abc-final");
```

#### GetLeaderboard(leaderboardId, limit?, offset?, period?, viewSlug?)

Fetch leaderboard entries.

```csharp
// Get top 10
_ascnd.GetLeaderboard("weekly-highscores");

// Paginate
_ascnd.GetLeaderboard("weekly-highscores", limit: 25, offset: 50);

// Filter by time period
_ascnd.GetLeaderboard("weekly-highscores", period: "current");

// Filter by metadata view
_ascnd.GetLeaderboard("weekly-highscores", viewSlug: "platform-pc");
```

#### GetPlayerRank(leaderboardId, playerId, period?, viewSlug?)

Get a specific player's rank and percentile.

```csharp
_ascnd.GetPlayerRank("weekly-highscores", "player-123");
```

#### Reinitialize()

Reinitialize the client if you change `ApiKey` or `BaseUrl` at runtime.

```csharp
_ascnd.ApiKey = "new-api-key";
_ascnd.Reinitialize();
```

### Signals

#### ScoreSubmitted(scoreId: string, rank: long, isNewBest: bool)

Emitted when a score is successfully submitted.

#### LeaderboardReceived(entries: Array, totalEntries: long, hasMore: bool)

Emitted when leaderboard data is received. Each entry in the array is a Dictionary with:
- `rank` (long)
- `playerId` (string)
- `score` (long)
- `submittedAt` (string, ISO 8601 format)
- `bracket` (Dictionary, optional) - Contains `id`, `name`, `color`

#### PlayerRankReceived(rank: long, score: long, percentile: float)

Emitted when a player's rank is received.

#### RequestFailed(operation: string, error: string)

Emitted when any API request fails. `operation` indicates which method failed.

## GDScript Usage

The addon also works from GDScript:

```gdscript
extends Node

@onready var ascnd = $AscndClient

func _ready():
    ascnd.score_submitted.connect(_on_score_submitted)
    ascnd.leaderboard_received.connect(_on_leaderboard_received)
    ascnd.request_failed.connect(_on_request_failed)

func submit_score(score: int):
    ascnd.SubmitScore("my-leaderboard", "player-123", score)

func _on_score_submitted(score_id: String, rank: int, is_new_best: bool):
    print("Rank: #%d" % rank)

func _on_leaderboard_received(entries: Array, total: int, has_more: bool):
    for entry in entries:
        print("#%d: %s - %d" % [entry.rank, entry.playerId, entry.score])

func _on_request_failed(operation: String, error: String):
    push_error("API error: " + error)
```

## Requirements

- Godot 4.2+ with .NET support
- .NET 8.0 SDK

## Links

- [Documentation](https://ascnd.gg/docs/sdks/godot)
- [Ascnd Dashboard](https://dashboard.ascnd.gg)
- [GitHub](https://github.com/ascnd-gg/ascnd-client-godot)
- [C# SDK](https://github.com/ascnd-gg/ascnd-client-csharp)

## License

MIT License - see [LICENSE](LICENSE) for details.
