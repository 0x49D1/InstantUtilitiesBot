dotnet restore
dotnet build
dotnet build -r ubuntu.16.04-x64
dotnet publish -c release -r ubuntu.16.04-x64
