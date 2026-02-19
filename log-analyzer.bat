@echo off
dotnet run --project "%~dp0\log-analyzer\log-analyzer.csproj" -v q -- %*
