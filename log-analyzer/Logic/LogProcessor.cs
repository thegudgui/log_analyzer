using System.Globalization;
using System.Text.RegularExpressions;
using LogAnalyzer.Models;

namespace LogAnalyzer.Logic;

public static class LogProcessor
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "from", "this", "that", "have", "has", "not", "but", "you",
        "are", "was", "were", "will", "can", "into", "onto", "over", "under", "between", "a",
        "an", "of", "to", "in", "on", "at", "by", "is", "it", "as", "be", "or"
    };

    public static LogEntry? ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        string[] parts = line.Split(' ', 3);
        if (parts.Length < 3) return null;

        if (!DateTimeOffset.TryParse(parts[0], null, DateTimeStyles.AssumeUniversal, out var timestamp))
        {
            return null;
        }

        if (!Enum.TryParse<LogLevel>(parts[1], true, out var level))
        {
            return null;
        }

        return new LogEntry(timestamp, level, parts[2]);
    }

    public static IEnumerable<string> GetValidWords(string message)
    {
        return LogRegex.WordsSplitter().Split(message)
            .Select(w => w.ToLowerInvariant())
            .Where(w => w.Length >= 3 && !StopWords.Contains(w));
    }
}

internal static partial class LogRegex
{
    [GeneratedRegex(@"[^a-zA-Z]+")]
    public static partial Regex WordsSplitter();
}
