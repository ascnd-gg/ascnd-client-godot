using Godot;
using System;
using Ascnd.Godot;

public partial class Main : Control
{
    private AscndClient _ascnd = null!;
    private Label _resultLabel = null!;
    private Random _random = new();

    public override void _Ready()
    {
        _ascnd = GetNode<AscndClient>("AscndClient");
        _resultLabel = GetNode<Label>("VBoxContainer/ResultLabel");

        // Connect signals
        _ascnd.ScoreSubmitted += OnScoreSubmitted;
        _ascnd.LeaderboardReceived += OnLeaderboardReceived;
        _ascnd.PlayerRankReceived += OnPlayerRankReceived;
        _ascnd.RequestFailed += OnRequestFailed;

        // Check if API key is configured
        if (string.IsNullOrEmpty(_ascnd.ApiKey))
        {
            _resultLabel.Text = "Set your API key in the AscndClient node's Inspector!";
        }
    }

    public void OnSubmitPressed()
    {
        var score = _random.Next(1000, 100000);
        var playerId = $"player-{_random.Next(1, 100)}";

        _resultLabel.Text = $"Submitting score {score} for {playerId}...";
        _ascnd.SubmitScore("demo-leaderboard", playerId, score);
    }

    public void OnLeaderboardPressed()
    {
        _resultLabel.Text = "Fetching leaderboard...";
        _ascnd.GetLeaderboard("demo-leaderboard", limit: 5);
    }

    private void OnScoreSubmitted(string scoreId, int rank, bool isNewBest)
    {
        var bestText = isNewBest ? " (New personal best!)" : "";
        _resultLabel.Text = $"Score submitted!\nRank: #{rank}{bestText}\nScore ID: {scoreId}";
    }

    private void OnLeaderboardReceived(
        Godot.Collections.Array entries,
        int totalEntries,
        bool hasMore)
    {
        var text = $"Leaderboard ({totalEntries} total entries):\n\n";

        foreach (Godot.Collections.Dictionary entry in entries)
        {
            text += $"#{entry["rank"]}: {entry["playerId"]} - {entry["score"]}\n";
        }

        if (hasMore)
        {
            text += "\n(more entries available)";
        }

        _resultLabel.Text = text;
    }

    private void OnPlayerRankReceived(int rank, long score, string percentile)
    {
        _resultLabel.Text = $"Your rank: #{rank}\nScore: {score}\nPercentile: {percentile}";
    }

    private void OnRequestFailed(string operation, string error)
    {
        _resultLabel.Text = $"Error in {operation}:\n{error}";
        GD.PrintErr($"API Error [{operation}]: {error}");
    }
}
