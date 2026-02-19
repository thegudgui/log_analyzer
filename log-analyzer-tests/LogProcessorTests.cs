using LogAnalyzer.Logic;
using LogAnalyzer.Models;

namespace log_analyzer_tests;

public class LogProcessorTests
{
    public class ParseLine
    {
        [Fact]
        public void ValidInput_ReturnsLogEntry()
        {
            var line = "2025-09-18T14:32:10Z INFO User logged in";

            var result = LogProcessor.ParseLine(line);

            Assert.NotNull(result);
            Assert.Equal(LogLevel.INFO, result.Level);
            Assert.Equal("User logged in", result.Message);
            Assert.Equal(DateTimeOffset.Parse("2025-09-18T14:32:10Z"), result.Timestamp);
        }

        [Fact]
        public void AllLogLevels_ParseCorrectly()
        {
            var levels = new[] { "TRACE", "DEBUG", "INFO", "WARN", "ERROR", "FATAL" };

            foreach (var level in levels)
            {
                var line = $"2025-09-18T14:32:10Z {level} Some message";
                var result = LogProcessor.ParseLine(line);

                Assert.NotNull(result);
                Assert.Equal(Enum.Parse<LogLevel>(level), result.Level);
            }
        }

        [Fact]
        public void CaseInsensitiveLevel_ParsesCorrectly()
        {
            var line = "2025-09-18T14:32:10Z info User logged in";

            var result = LogProcessor.ParseLine(line);

            Assert.NotNull(result);
            Assert.Equal(LogLevel.INFO, result.Level);
        }

        [Fact]
        public void TimezoneOffset_ParsesCorrectly()
        {
            var line = "2025-09-18T14:32:10+05:30 ERROR Timeout occurred";

            var result = LogProcessor.ParseLine(line);

            Assert.NotNull(result);
            Assert.Equal(LogLevel.ERROR, result.Level);
            Assert.Equal(new TimeSpan(5, 30, 0), result.Timestamp.Offset);
        }

        [Fact]
        public void MessageWithMultipleSpaces_PreservesFullMessage()
        {
            var line = "2025-09-18T14:32:10Z WARN Disk space  is   low";

            var result = LogProcessor.ParseLine(line);

            Assert.NotNull(result);
            Assert.Equal("Disk space  is   low", result.Message);
        }

        [Fact]
        public void MalformedDate_ReturnsNull()
        {
            var line = "09/18/2025 14:32:10 INFO User logged in";

            var result = LogProcessor.ParseLine(line);

            Assert.Null(result);
        }

        [Fact]
        public void InvalidLevel_ReturnsNull()
        {
            var line = "2025-09-18T14:32:10Z NOTALEVEL User logged in";

            var result = LogProcessor.ParseLine(line);

            Assert.Null(result);
        }

        [Fact]
        public void EmptyString_ReturnsNull()
        {
            Assert.Null(LogProcessor.ParseLine(""));
        }

        [Fact]
        public void WhitespaceOnly_ReturnsNull()
        {
            Assert.Null(LogProcessor.ParseLine("   "));
        }

        [Fact]
        public void MissingMessage_ReturnsNull()
        {
            var line = "2025-09-18T14:32:10Z INFO";

            var result = LogProcessor.ParseLine(line);

            Assert.Null(result);
        }

        [Fact]
        public void TimestampOnly_ReturnsNull()
        {
            var line = "2025-09-18T14:32:10Z";

            var result = LogProcessor.ParseLine(line);

            Assert.Null(result);
        }

        [Fact]
        public void GarbageInput_ReturnsNull()
        {
            Assert.Null(LogProcessor.ParseLine("not a log line at all"));
        }
    }

    public class Analyze
    {
        [Fact]
        public void Integration_CorrectlyAnalyzesMultipleLines()
        {
            // Arrange
            var lines = new[]
            {
                "2025-01-01T10:00:00Z INFO Hello World",
                "bad line",
                "2025-01-01T10:05:00Z ERROR Database failed",
                "2025-01-01T10:05:00Z ERROR Connection lost" // Tie-break case
            };

            // Act
            var report = LogAggregator.Analyze(lines);

            // Assert
            Assert.Equal(3, report.TotalCount);
            Assert.Equal(1, report.MalformedCount);
            Assert.Equal(1, report.LevelCounts[LogLevel.INFO]);
            Assert.Equal(2, report.LevelCounts[LogLevel.ERROR]);
            Assert.Equal("Connection lost", report.MostRecentErrorMessage);
            Assert.Contains("hello", report.TopInfoWords);
            Assert.Contains("world", report.TopInfoWords);
        }
    }

    public class GetValidWords
    {
        [Fact]
        public void FiltersStopWords()
        {
            var message = "the apple and a cat or between theater";

            var result = LogProcessor.GetValidWords(message).ToList();

            Assert.Contains("apple", result);
            Assert.Contains("cat", result);
            Assert.Contains("theater", result);
            Assert.DoesNotContain("the", result);
            Assert.DoesNotContain("and", result);
            Assert.DoesNotContain("between", result);
        }

        [Fact]
        public void FiltersWordsUnderThreeCharacters()
        {
            var message = "I am ok but running fine";

            var result = LogProcessor.GetValidWords(message).ToList();

            Assert.DoesNotContain("i", result);
            Assert.DoesNotContain("am", result);
            Assert.DoesNotContain("ok", result);
            Assert.Contains("running", result);
            Assert.Contains("fine", result);
        }

        [Fact]
        public void SplitsOnNonLetterCharacters()
        {
            var message = "User's login (v2) failed!";

            var result = LogProcessor.GetValidWords(message).ToList();

            Assert.Contains("user", result);
            Assert.Contains("login", result);
            Assert.Contains("failed", result);
            // "v" is 1 char after splitting on '2', so it's filtered out
            Assert.DoesNotContain("v", result);
        }

        [Fact]
        public void ConvertsToLowercase()
        {
            var message = "Server STARTED Successfully";

            var result = LogProcessor.GetValidWords(message).ToList();

            Assert.Contains("server", result);
            Assert.Contains("started", result);
            Assert.Contains("successfully", result);
            Assert.DoesNotContain("Server", result);
            Assert.DoesNotContain("STARTED", result);
        }

        [Fact]
        public void EmptyMessage_ReturnsEmpty()
        {
            var result = LogProcessor.GetValidWords("").ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void AllStopWords_ReturnsEmpty()
        {
            var message = "the and for with from";

            var result = LogProcessor.GetValidWords(message).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void NumbersOnlyMessage_ReturnsEmpty()
        {
            var message = "12345 678 90";

            var result = LogProcessor.GetValidWords(message).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void MixedPunctuationAndWords_ExtractsValidWords()
        {
            var message = "error---connection::timeout [retrying]";

            var result = LogProcessor.GetValidWords(message).ToList();

            Assert.Contains("error", result);
            Assert.Contains("connection", result);
            Assert.Contains("timeout", result);
            Assert.Contains("retrying", result);
        }
    }
}
