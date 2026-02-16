# Makefile for log-analyzer (Windows, using PowerShell)

SOLUTION = log-analyzer.slnx
PROJECT = log-analyzer/log-analyzer.csproj
PUBLISH_DIR = publish
EXE = $(PUBLISH_DIR)/log-analyzer.exe

.PHONY: build publish run clean test coverage report all

all: build publish

build:
	dotnet build $(SOLUTION)

publish:
	dotnet publish $(PROJECT) -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $(PUBLISH_DIR)

run:
	cd $(PUBLISH_DIR) && ./log-analyzer.exe

test:
	dotnet test log-analyzer-tests/log-analyzer-tests.csproj

coverage:
	dotnet test log-analyzer-tests/log-analyzer-tests.csproj --collect:"XPlat Code Coverage" --results-directory ./coverage

report:
	reportgenerator -reports:"coverage/*/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
	cmd /c start coveragereport/index.html

clean:
	rm -rf $(PUBLISH_DIR) coverage coveragereport
	dotnet clean $(SOLUTION)
