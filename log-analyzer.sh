#!/bin/bash
dotnet run --project "$(dirname "$0")/log-analyzer/log-analyzer.csproj" -v q -- "$@"
