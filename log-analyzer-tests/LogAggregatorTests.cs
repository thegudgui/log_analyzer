using LogAnalyzer.Logic;
using LogAnalyzer.Models;

namespace log_analyzer_tests;

public class LogAggregatorTests
{
    [Fact]
    public void Aggregator_CorrectlyCountsEntries()
    {
        // Arrange
        var agg = new LogAggregator();
        
        // Act
        agg.ProcessLine("2025-01-01T10:00:00Z INFO Hello");
        agg.ProcessLine("invalid line");
        agg.ProcessLine("2025-01-01T10:00:01Z ERROR Problem");

        // Assert
        Assert.Equal(3, agg.TotalCount);
        Assert.Equal(1, agg.MalformedCount);
        Assert.Equal(1, agg.GetLevelCounts()[LogLevel.INFO]);
        Assert.Equal(1, agg.GetLevelCounts()[LogLevel.ERROR]);
    }

    [Fact]
    public void Aggregator_ErrorTieBreak_ReturnsLastEncountered()
    {
        // Arrange
        var agg = new LogAggregator();
        var timestamp = "2025-01-01T10:00:00Z";
        
        // Act: Two errors with exact same timestamp
        agg.ProcessLine($"{timestamp} ERROR First Error");
        agg.ProcessLine($"{timestamp} ERROR Second Error");

        // Assert: The second one should win
        Assert.Equal("Second Error", agg.MostRecentError?.Message);
    }

    [Fact]
    public void Aggregator_HigherTimestamp_UpdatesMostRecentError()
    {
        // Arrange
        var agg = new LogAggregator();
        
        // Act
        agg.ProcessLine("2025-01-01T10:00:00Z ERROR Early");
        agg.ProcessLine("2025-01-01T11:00:00Z ERROR Late");

        // Assert
        Assert.Equal("Late", agg.MostRecentError?.Message);
    }

    [Fact]
    public void Aggregator_Top3Words_AlphabeticalTieBreak()
    {
        // Arrange
        var agg = new LogAggregator();
        
        // Act: 'apple' and 'zebra' appear twice each
        agg.ProcessLine("2025-01-01T10:00:00Z INFO apple apple zebra zebra banana");
        
        var topWords = agg.GetTop3InfoWords().ToList();

        // Assert: 
        // 1. apple (2)
        // 2. zebra (2) -- comes second because of alphabetical ThenBy
        // 3. banana (1)
        Assert.Equal("apple", topWords[0]);
        Assert.Equal("zebra", topWords[1]);
        Assert.Equal("banana", topWords[2]);
    }

    [Fact]
    public void Aggregator_Top3Words_FiltersStopWords()
    {
        // Arrange
        var agg = new LogAggregator();
        
        // Act
        agg.ProcessLine("2025-01-01T10:00:00Z INFO the quick brown fox");
        
        var topWords = agg.GetTop3InfoWords().ToList();

        // Assert
        Assert.DoesNotContain("the", topWords);
        Assert.Contains("quick", topWords);
        Assert.Contains("brown", topWords);
        Assert.Contains("fox", topWords);
    }

    [Fact]
    public void Aggregator_CreateReport_ReturnsConsistentData()
    {
        // Arrange
        var agg = new LogAggregator();
        agg.ProcessLine("2025-01-01T10:00:00Z ERROR My Error");
        agg.ProcessLine("2025-01-01T10:00:00Z INFO Logged In");

        // Act
        var report = agg.CreateReport();

        // Assert
        Assert.Equal(2, report.TotalCount);
        Assert.Equal("My Error", report.MostRecentErrorMessage);
        Assert.Equal(1, report.LevelCounts[LogLevel.ERROR]);
        Assert.Contains("logged", report.TopInfoWords);
    }

    [Fact]
    public void Aggregator_NoErrors_ReturnsNA()
    {
        var agg = new LogAggregator();
        agg.ProcessLine("2025-01-01T10:00:00Z INFO Just Info");
        
        var report = agg.CreateReport();
        
        Assert.Equal("N/A", report.MostRecentErrorMessage);
    }

    [Fact]
    public void Aggregator_SingleWord_ReturnsSingleWord()
    {
        var agg = new LogAggregator();
        agg.ProcessLine("2025-01-01T10:00:00Z INFO hello");
        
        var topWords = agg.GetTop3InfoWords().ToList();
        
        Assert.Single(topWords);
        Assert.Equal("hello", topWords[0]);
    }

    [Fact]
    public void Aggregator_EmptyLines_ReturnsZeroedReport()
    {
        var report = LogAggregator.Analyze(Enumerable.Empty<string>());
        
        Assert.Equal(0, report.TotalCount);
        Assert.Equal("N/A", report.MostRecentErrorMessage);
        Assert.Empty(report.TopInfoWords);
    }

    [Fact]
    public void Aggregator_TimestampWinsOverOrder_WhenTimestampIsLater()
    {
        var agg = new LogAggregator();
        
        // Late timestamp first, Early timestamp second
        agg.ProcessLine("2025-01-01T12:00:00Z ERROR Late");
        agg.ProcessLine("2025-01-01T10:00:00Z ERROR Early");

        Assert.Equal("Late", agg.MostRecentError?.Message);
    }
}
