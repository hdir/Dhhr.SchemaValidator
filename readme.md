# Validate XML
This is a console app for validating XML messages using xsd.
The app is bundled with a subset of messages from Helsedirektoratet.

# Run the precompiled .dll
## Prerequisites
You'll need the .NET Core Runtime (or SDK).
Download it from here if it's not already installed on your system: https://dotnet.microsoft.com/download

## Validate message
1. Open PowerShell
2. Run `dotnet Dhhr.SchemaValidator.Cli.dll -h` for options
3. Run `dotnet Dhhr.SchemaValidator.Cli.dll -f path/to/file.xml`


# How to run from code
## Prerequisites
You'll need the .NET Core or SDK.
Download it from here if it's not already installed on your system: https://dotnet.microsoft.com/download

## Validate message
1. Open PowerShell to `src/Dhhr.SchemaValidator.Cli`
2. Run `dotnet run -- -h` for options
3. Run `dotnet run -- -f path/to/file.xml`
