using LogAnalyzer.Models;

namespace LogAnalyzer.Logic;

public static class ReportPrinter
{
    public static void PrintToConsole(LogReport report)
    {
        WriteReport(Console.Out, report);
    }

    public static void WriteReport(TextWriter writer, LogReport report)
    {
        writer.WriteLine($"Total Entries: {report.TotalCount}");
        
        foreach (LogLevel level in Enum.GetValues<LogLevel>())
        {
            writer.WriteLine($"{level}: {report.LevelCounts[level]}");
        }
        
        writer.WriteLine($"Malformed: {report.MalformedCount}");
        writer.WriteLine($"Most Recent ERROR: {report.MostRecentErrorMessage}");
        writer.WriteLine($"Top 3 Frequent Words (INFO): {string.Join(", ", report.TopInfoWords)}");
    }
}
