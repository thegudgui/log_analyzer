namespace LogAnalyzer.Models;

public record LogReport(
    int TotalCount,
    IReadOnlyDictionary<LogLevel, int> LevelCounts,
    int MalformedCount,
    string MostRecentErrorMessage,
    IEnumerable<string> TopInfoWords
);
