$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $root '.dotnet-home'
$env:NUGET_PACKAGES = Join-Path $root '.nuget\packages'
$env:APPDATA = Join-Path $root '.appdata'

dotnet test (Join-Path $root 'GameMacro.sln') -c Release
dotnet publish (Join-Path $root 'src\GameMacro.App\GameMacro.App.csproj') `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=false `
  -o (Join-Path $root 'artifacts\win-x64-auto')
