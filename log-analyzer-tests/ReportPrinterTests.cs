using LogAnalyzer.Logic;
using LogAnalyzer.Models;
using System.Text;

namespace log_analyzer_tests;

public class ReportPrinterTests
{
    [Fact]
    public void WriteReport_OutputsCorrectFormatAndOrder()
    {
        // Arrange
        var levelCounts = Enum.GetValues<LogLevel>()
            .ToDictionary(l => l, l => (l == LogLevel.INFO) ? 5 : 0);

        var report = new LogReport(
            TotalCount: 10,
            LevelCounts: levelCounts,
            MalformedCount: 2,
            MostRecentErrorMessage: "Database fail",
            TopInfoWords: new[] { "user", "login" }
        );

        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);

        // Act
        ReportPrinter.WriteReport(writer, report);

        // Assert
        var lines = sb.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Total Entries: 10", lines[0]);
        Assert.Equal("TRACE: 0", lines[1]);
        Assert.Equal("DEBUG: 0", lines[2]);
        Assert.Equal("INFO: 5", lines[3]);
        Assert.Equal("WARN: 0", lines[4]);
        Assert.Equal("ERROR: 0", lines[5]);
        Assert.Equal("FATAL: 0", lines[6]);
        Assert.Equal("Malformed: 2", lines[7]);
        Assert.Equal("Most Recent ERROR: Database fail", lines[8]);
        Assert.Equal("Top 3 Frequent Words (INFO): user, login", lines[9]);
    }

    [Fact]
    public void WriteReport_HandlesEmptyTopWords()
    {
        // Arrange
        var levelCounts = Enum.GetValues<LogLevel>().ToDictionary(l => l, _ => 0);
        var report = new LogReport(0, levelCounts, 0, "N/A", Enumerable.Empty<string>());
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);

        // Act
        ReportPrinter.WriteReport(writer, report);

        // Assert
        var output = sb.ToString();
        Assert.Contains("Top 3 Frequent Words (INFO): ", output);
        // Should end with just the label if no words exist
        Assert.EndsWith($"Top 3 Frequent Words (INFO): {Environment.NewLine}", output);
    }

    [Fact]
    public void WriteReport_ThreeWords_FormattedWithCommas()
    {
        // Arrange
        var levelCounts = Enum.GetValues<LogLevel>().ToDictionary(l => l, _ => 0);
        var report = new LogReport(1, levelCounts, 0, "N/A", new[] { "one", "two", "three" });
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);

        // Act
        ReportPrinter.WriteReport(writer, report);

        // Assert
        Assert.Contains("Top 3 Frequent Words (INFO): one, two, three", sb.ToString());
    }
}
