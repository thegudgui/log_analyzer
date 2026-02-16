using System;
using System.IO;
using LogAnalyzer.Models;
using LogAnalyzer.Logic;

if (args.Length == 0)
{
    Console.WriteLine("Usage: log-analyzer <path-to-logfile>");
    return;
}

string filePath = args[0];

if (!File.Exists(filePath))
{
    Console.Error.WriteLine($"Error: File not found at {filePath}");
    Environment.Exit(2);
}

try
{
    ProcessLogFile(filePath);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error reading file: {ex.Message}");
    Environment.Exit(2);
}

static void ProcessLogFile(string path)
{
    var report = LogAggregator.Analyze(File.ReadLines(path));
    ReportPrinter.PrintToConsole(report);
}
