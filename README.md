# log-analyzer

## Overview
log-analyzer is a C# console application designed to analyze log files. This repository contains the main application and a separate xUnit test project.

## Project Structure
- `log-analyzer/` - Main console application project
- `log-analyzer-tests/` - xUnit test project for unit testing


## Build Instructions
1. Ensure you have the .NET SDK installed (version 10.0.2 or later).
2. Open a terminal in the project root directory.
3. To restore dependencies and build the solution, run:
   ```sh
   dotnet build log-analyzer.sln
   ```
4. To compile the application as a standalone Windows executable, run:
   ```sh
   dotnet publish log-analyzer/log-analyzer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
   ```
   This will create a single-file executable in the `publish` folder.

## Run Instructions

To run the main application as a standalone executable:

1. Navigate to the `publish` folder:
   ```sh
   cd publish
   ```
2. Run the app using the batch file (recommended for Windows):
   ```sh
   log-analyzer
   ```
   Or run the executable directly:
   ```sh
   log-analyzer.exe
   ```

To run from any terminal, add the `publish` folder to your system PATH or copy `log-analyzer.bat` and `log-analyzer.exe` to a directory already in your PATH. This allows you to simply type `log-analyzer` from anywhere.

You can also run the app using the .NET CLI (for development):
```sh
dotnet run --project log-analyzer/log-analyzer.csproj
```

## Example Output
Running the analyzer on a sample log file will produce output similar to the following:
```text
Total Entries: 20
TRACE: 0
DEBUG: 0
INFO: 12
WARN: 3
ERROR: 4
FATAL: 1
Malformed: 0
Most Recent ERROR: Connection to server 'srv-01' failed
Top 3 Frequent Words (INFO): data, received, user
```

## Test Instructions
To run all unit tests:

```sh
dotnet test
```

## Design Trade-offs
- **Streaming Processing**: Used `File.ReadLines` to process logs line-by-line. This ensures the application can handle multi-gigabyte log files with a constant, low memory footprint, rather than loading the entire file into RAM.
- **Performance Optimization**: Used .NET Source Generators (`GeneratedRegex`) for word splitting and `HashSet<string>` for stop-word lookups, ensuring $O(N)$ performance for log analysis.
- **Strict Parsing**: Any line not matching the `[ISO-8601] [LEVEL] [MESSAGE]` format is categorized as "Malformed" to maintain report integrity, rather than crashing or guessing the content.
- **Stability**: Segregated logic into `LogAnalyzer.Logic` and models into `LogAnalyzer.Models` to support robust unit testing with xUnit, decoupled from the console I/O.
- **Tie-breaking**: In cases where multiple ERROR logs have the exact same timestamp, the application reports the one that appears *last* in the file to ensure the most recent state is captured.

## Code Coverage
To view detailed code coverage:
1. Run `dotnet test log-analyzer-tests/log-analyzer-tests.csproj --collect:"XPlat Code Coverage" --results-directory ./coverage`
2. Run `reportgenerator -reports:"coverage/*/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html`
3. Open `coveragereport/index.html` in your browser.
