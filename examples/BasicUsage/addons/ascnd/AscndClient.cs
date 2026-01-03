using Godot;
using System;
using System.Threading.Tasks;
using Ascnd.Client;
using Ascnd.Client.Grpc;

namespace Ascnd.Godot;

/// <summary>
/// Godot node wrapper for the Ascnd leaderboard API client.
/// Provides signals for async operations and integrates with Godot's node lifecycle.
/// </summary>
public partial class AscndClient : Node
{
    #region Exports

    /// <summary>
    /// Your Ascnd API key. Obtain from https://dashboard.ascnd.gg
    /// </summary>
    [Export]
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// The Ascnd API base URL. Usually you don't need to change this.
    /// </summary>
    [Export]
    public string BaseUrl { get; set; } = "https://api.ascnd.gg";

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    [Export]
    public int TimeoutSeconds { get; set; } = 30;

    #endregion

    #region Signals

    /// <summary>
    /// Emitted when a score is successfully submitted.
    /// </summary>
    /// <param name="scoreId">The unique ID of the submitted score</param>
    /// <param name="rank">The player's new rank on the leaderboard</param>
    /// <param name="isNewBest">True if this is the player's new personal best</param>
    [Signal]
    public delegate void ScoreSubmittedEventHandler(string scoreId, long rank, bool isNewBest);

    /// <summary>
    /// Emitted when leaderboard data is received.
    /// </summary>
    /// <param name="entries">Array of dictionaries with rank, playerId, score keys</param>
    /// <param name="totalEntries">Total number of entries on the leaderboard</param>
    /// <param name="hasMore">True if there are more entries available</param>
    [Signal]
    public delegate void LeaderboardReceivedEventHandler(Godot.Collections.Array<Godot.Collections.Dictionary> entries, long totalEntries, bool hasMore);

    /// <summary>
    /// Emitted when a player's rank is received.
    /// </summary>
    /// <param name="rank">The player's rank (0 if not ranked)</param>
    /// <param name="score">The player's score</param>
    /// <param name="percentile">The player's percentile (0-100)</param>
    [Signal]
    public delegate void PlayerRankReceivedEventHandler(long rank, long score, float percentile);

    /// <summary>
    /// Emitted when any API request fails.
    /// </summary>
    /// <param name="operation">The operation that failed (e.g., "SubmitScore")</param>
    /// <param name="error">Error message describing the failure</param>
    [Signal]
    public delegate void RequestFailedEventHandler(string operation, string error);

    #endregion

    private Client.AscndClient? _client;

    public override void _Ready()
    {
        InitializeClient();
    }

    public override void _ExitTree()
    {
        _client?.Dispose();
        _client = null;
    }

    /// <summary>
    /// Reinitialize the client with current export values.
    /// Call this if you change ApiKey or BaseUrl at runtime.
    /// </summary>
    public void Reinitialize()
    {
        _client?.Dispose();
        InitializeClient();
    }

    private void InitializeClient()
    {
        if (string.IsNullOrEmpty(ApiKey))
        {
            GD.PushWarning("AscndClient: ApiKey is not set. Set it in the Inspector or via code.");
            return;
        }

        var options = new AscndClientOptions(ApiKey, BaseUrl)
        {
            TimeoutSeconds = TimeoutSeconds
        };

        _client = new Client.AscndClient(options);
    }

    #region Public API

    /// <summary>
    /// Submit a score to a leaderboard. Emits ScoreSubmitted or RequestFailed signal.
    /// </summary>
    /// <param name="leaderboardId">The leaderboard identifier</param>
    /// <param name="playerId">The player's unique identifier</param>
    /// <param name="score">The score value</param>
    /// <param name="metadata">Optional metadata as a JSON string</param>
    /// <param name="idempotencyKey">Optional key to prevent duplicate submissions</param>
    public async void SubmitScore(string leaderboardId, string playerId, long score, string metadata = "", string idempotencyKey = "")
    {
        if (!EnsureClient("SubmitScore")) return;

        try
        {
            var request = new SubmitScoreRequest
            {
                LeaderboardId = leaderboardId,
                PlayerId = playerId,
                Score = score
            };

            if (!string.IsNullOrEmpty(metadata))
            {
                request.Metadata = Google.Protobuf.ByteString.CopyFromUtf8(metadata);
            }

            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                request.IdempotencyKey = idempotencyKey;
            }

            var response = await _client!.SubmitScoreAsync(request);

            CallDeferred(MethodName.EmitSignal, SignalName.ScoreSubmitted,
                response.ScoreId, response.Rank, response.IsNewBest);
        }
        catch (Exception ex)
        {
            CallDeferred(MethodName.EmitSignal, SignalName.RequestFailed,
                "SubmitScore", ex.Message);
        }
    }

    /// <summary>
    /// Fetch leaderboard entries. Emits LeaderboardReceived or RequestFailed signal.
    /// </summary>
    /// <param name="leaderboardId">The leaderboard identifier</param>
    /// <param name="limit">Maximum number of entries to fetch (default 10, max 100)</param>
    /// <param name="offset">Number of entries to skip for pagination</param>
    /// <param name="period">Time period: "current", "previous", or ISO timestamp</param>
    /// <param name="viewSlug">Optional view slug for filtered leaderboards</param>
    public async void GetLeaderboard(string leaderboardId, int limit = 10, int offset = 0, string period = "", string viewSlug = "")
    {
        if (!EnsureClient("GetLeaderboard")) return;

        try
        {
            var request = new GetLeaderboardRequest
            {
                LeaderboardId = leaderboardId,
                Limit = limit,
                Offset = offset
            };

            if (!string.IsNullOrEmpty(period))
            {
                request.Period = period;
            }

            if (!string.IsNullOrEmpty(viewSlug))
            {
                request.ViewSlug = viewSlug;
            }

            var response = await _client!.GetLeaderboardAsync(request);

            var entries = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            foreach (var entry in response.Entries)
            {
                var dict = new Godot.Collections.Dictionary
                {
                    ["rank"] = entry.Rank,
                    ["playerId"] = entry.PlayerId,
                    ["score"] = entry.Score,
                    ["submittedAt"] = entry.SubmittedAt?.ToDateTime().ToString("o") ?? ""
                };

                if (entry.Bracket != null)
                {
                    dict["bracket"] = new Godot.Collections.Dictionary
                    {
                        ["id"] = entry.Bracket.Id,
                        ["name"] = entry.Bracket.Name,
                        ["color"] = entry.Bracket.Color
                    };
                }

                entries.Add(dict);
            }

            CallDeferred(MethodName.EmitSignal, SignalName.LeaderboardReceived,
                entries, response.TotalEntries, response.HasMore);
        }
        catch (Exception ex)
        {
            CallDeferred(MethodName.EmitSignal, SignalName.RequestFailed,
                "GetLeaderboard", ex.Message);
        }
    }

    /// <summary>
    /// Get a specific player's rank. Emits PlayerRankReceived or RequestFailed signal.
    /// </summary>
    /// <param name="leaderboardId">The leaderboard identifier</param>
    /// <param name="playerId">The player's unique identifier</param>
    /// <param name="period">Time period: "current", "previous", or ISO timestamp</param>
    /// <param name="viewSlug">Optional view slug for filtered leaderboards</param>
    public async void GetPlayerRank(string leaderboardId, string playerId, string period = "", string viewSlug = "")
    {
        if (!EnsureClient("GetPlayerRank")) return;

        try
        {
            var request = new GetPlayerRankRequest
            {
                LeaderboardId = leaderboardId,
                PlayerId = playerId
            };

            if (!string.IsNullOrEmpty(period))
            {
                request.Period = period;
            }

            if (!string.IsNullOrEmpty(viewSlug))
            {
                request.ViewSlug = viewSlug;
            }

            var response = await _client!.GetPlayerRankAsync(request);

            CallDeferred(MethodName.EmitSignal, SignalName.PlayerRankReceived,
                response.Rank, response.Score, (float)response.Percentile);
        }
        catch (Exception ex)
        {
            CallDeferred(MethodName.EmitSignal, SignalName.RequestFailed,
                "GetPlayerRank", ex.Message);
        }
    }

    #endregion

    private bool EnsureClient(string operation)
    {
        if (_client != null) return true;

        if (string.IsNullOrEmpty(ApiKey))
        {
            CallDeferred(MethodName.EmitSignal, SignalName.RequestFailed,
                operation, "ApiKey is not configured");
        }
        else
        {
            // Try to initialize if we have an API key
            InitializeClient();
            if (_client != null) return true;

            CallDeferred(MethodName.EmitSignal, SignalName.RequestFailed,
                operation, "Failed to initialize client");
        }

        return false;
    }
}
