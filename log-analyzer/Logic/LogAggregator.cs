using LogAnalyzer.Models;

namespace LogAnalyzer.Logic;

public class LogAggregator
{
    public int TotalCount { get; private set; }
    public int MalformedCount { get; private set; }
    public LogEntry? MostRecentError { get; private set; }
    
    private readonly Dictionary<LogLevel, int> _levelCounts = 
        Enum.GetValues<LogLevel>().ToDictionary(l => l, _ => 0);
    
    private readonly Dictionary<string, int> _infoWordCounts = new();

    public static LogReport Analyze(IEnumerable<string> lines)
    {
        var aggregator = new LogAggregator();
        foreach (var line in lines)
        {
            aggregator.ProcessLine(line);
        }
        return aggregator.CreateReport();
    }

    public void ProcessLine(string line)
    {
        var entry = LogProcessor.ParseLine(line);
        if (entry == null)
        {
            MalformedCount++;
            return;
        }

        TotalCount++;

        if (entry.Level == LogLevel.ERROR)
        {
            // Tie-break: Last one encountered wins for identical timestamps
            if (MostRecentError == null || entry.Timestamp >= MostRecentError.Timestamp)
            {
                MostRecentError = entry;
            }
        }

        if (entry.Level == LogLevel.INFO)
        {
            foreach (var word in LogProcessor.GetValidWords(entry.Message))
            {
                _infoWordCounts[word] = _infoWordCounts.GetValueOrDefault(word) + 1;
            }
        }

        _levelCounts[entry.Level]++;
    }

    public IReadOnlyDictionary<LogLevel, int> GetLevelCounts() => _levelCounts;

    public IEnumerable<string> GetTop3InfoWords()
    {
        return _infoWordCounts
            .OrderByDescending(p => p.Value)
            .ThenBy(p => p.Key) // Alphabetical tie-breaker
            .Take(3)
            .Select(p => p.Key);
    }

    public LogReport CreateReport()
    {
        return new LogReport(
            TotalCount,
            _levelCounts,
            MalformedCount,
            MostRecentError?.Message ?? "N/A",
            GetTop3InfoWords()
        );
    }
}
