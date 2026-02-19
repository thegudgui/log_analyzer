# log-analyzer

A C# console application that parses structured log files and generates a summary report including entry counts by severity level, malformed line detection, most recent error identification, and top frequent words in INFO messages.

## Project Structure

```
log-analyzer/                  # Main console application
├── Program.cs                 # CLI entry point and orchestration
├── Logic/
│   ├── LogProcessor.cs        # Line parsing and word tokenization
│   ├── LogAggregator.cs       # Stateful aggregation and report generation
│   └── ReportPrinter.cs       # Output formatting (TextWriter-based)
└── Models/
    ├── LogLevel.cs            # Enum: TRACE, DEBUG, INFO, WARN, ERROR, FATAL
    ├── LogEntry.cs            # Immutable record for a parsed log line
    └── LogReport.cs           # Immutable record for the final report

log-analyzer-tests/            # xUnit test project (34 tests)
├── LogProcessorTests.cs       # Parsing and word filtering tests
├── LogAggregatorTests.cs      # Aggregation, tie-breaking, and state tests
└── ReportPrinterTests.cs      # Output format verification tests

logs/                          # Sample log files for testing
```

## Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) or later

## Build Instructions

To restore dependencies and build the solution:
```sh
dotnet build
```

To compile as a self-contained Windows executable:
```sh
dotnet publish log-analyzer/log-analyzer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

## Run Instructions

To satisfy the `log-analyzer <path-to-logfile>` usage requirement, a `log-analyzer.bat` is provided in the root, which redirects to the application. Alternatively, the published executable is named `log-analyzer.exe`.

**Using the provided script (Windows Quick-Start):**
```cmd
.\log-analyzer.bat <path-to-logfile>
```

**Using the .NET CLI (recommended for development):**
```sh
dotnet run --project log-analyzer/log-analyzer.csproj -- <path-to-logfile>
```

**Using the published executable:**
```sh
./publish/log-analyzer.exe <path-to-logfile>
```

**Adding to PATH:**
To use `log-analyzer` globally from any directory:
1.  **Windows**: Add the folder containing `log-analyzer.bat` or the published executable to your User `PATH` environment variable.
2.  **Restart** your terminal.
3.  Run as: `log-analyzer <logfile>`

**Example:**
```sh
.\log-analyzer.bat logs/sample_log_20_entries.txt
```

**Exit Codes:**
- `0` — Success
- `2` — File not found or unreadable

## Example Output

Running the analyzer on the included sample log file (`logs/sample_log_20_entries.txt`):
```text
Total Entries: 20
TRACE: 3
DEBUG: 1
INFO: 9
WARN: 3
ERROR: 1
FATAL: 1
Malformed: 2
Most Recent ERROR: Unhandled exception
Top 3 Frequent Words (INFO): file, successfully, uploaded
```

## Test Instructions

To run all 34 unit and integration tests:
```sh
dotnet test
```

## Design Trade-offs

### Streaming vs. Loading Entire File
Used `File.ReadLines` to process logs line-by-line via lazy enumeration. This ensures the application can handle multi-gigabyte log files with a constant, low memory footprint — only one line is held in memory at a time, rather than loading the entire file into RAM with `File.ReadAllLines` or `File.ReadAllText`.

### Performance Optimization
- **Source-Generated Regex**: Used .NET `GeneratedRegex` for word splitting. This compiles the regex pattern at build time rather than at runtime, eliminating the overhead of runtime compilation and resolving the `SYSLIB1045` performance warning.
- **HashSet for Stop Words**: Stop-word lookups use a `HashSet<string>` with `StringComparer.OrdinalIgnoreCase`, providing O(1) average-case lookups instead of scanning through a list.

### Architecture: Separation of Concerns
The application is structured into three distinct layers:
- **`LogProcessor`** — Pure stateless utility: parses individual lines and tokenizes words. It has no knowledge of "the report" or accumulated state.
- **`LogAggregator`** — Stateful accumulator: iterates through lines, tracks counts, and builds the final `LogReport`. Owns the `Analyze` entry point because it manages the aggregation lifecycle.
- **`ReportPrinter`** — Output formatter: accepts a `TextWriter` abstraction instead of writing directly to `Console.Out`. This "Humble Object" pattern decouples formatting from I/O, allowing unit tests to verify exact output without capturing the console.

This separation was chosen over a single-file approach to keep each class focused on one responsibility, making them independently testable and easier to extend (e.g., supporting new log formats or output targets).

### Immutable Domain Models
Used C# `record` types for `LogEntry` and `LogReport`. Records provide value-based equality, immutability, and concise syntax. This ensures parsed data cannot be accidentally mutated after creation, which is important for thread safety and correctness in a streaming pipeline.

### Strict Parsing Strategy
Any line not matching the exact `[ISO-8601 timestamp] [LEVEL] [MESSAGE]` format is counted as "Malformed" rather than silently dropped or partially parsed. This approach favors data integrity over leniency — the report will always account for every line in the file, giving the operator confidence that no data was lost.

### Tie-breaking
- **Most Recent ERROR**: Uses `>=` comparison on timestamps so that when multiple ERROR entries share the same timestamp, the one appearing *last* in the file wins. This aligns with the assumption that file order reflects chronological order within the same second.
- **Top 3 Words**: Ties in word frequency are broken alphabetically (`ThenBy(p => p.Key)`), ensuring deterministic, reproducible output regardless of dictionary enumeration order.

### Testability
- **TextWriter Abstraction**: `ReportPrinter.WriteReport` accepts a `TextWriter` parameter, allowing tests to pass a `StringWriter` and assert against the exact formatted output without any console dependency.
- **Public Aggregator State**: `LogAggregator` exposes properties like `TotalCount` and `MostRecentError` for fine-grained assertions in unit tests, while `CreateReport` bundles everything into the immutable `LogReport` for integration-level verification.
- **34 tests** cover parsing edge cases (empty strings, missing fields, invalid timestamps, garbage input), aggregation logic (tie-breaking, word counting, stop-word filtering), and output formatting (exact line order, comma separation, empty word lists).

## Code Coverage

To generate a detailed HTML coverage report:
```sh
dotnet test log-analyzer-tests/log-analyzer-tests.csproj --collect:"XPlat Code Coverage" --results-directory ./coverage
reportgenerator -reports:"coverage/*/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```
Then open `coveragereport/index.html` in your browser.

> **Note:** `reportgenerator` is a .NET global tool. Install it with:
> `dotnet tool install -g dotnet-reportgenerator-globaltool`
