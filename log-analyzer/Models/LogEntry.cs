namespace LogAnalyzer.Models;

public record LogEntry(DateTimeOffset Timestamp, LogLevel Level, string Message);
