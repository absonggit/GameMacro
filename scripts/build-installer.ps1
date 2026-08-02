param(
    [string]$Version = '1.0.4'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\GameMacro.App\GameMacro.App.csproj'
$publishDirectory = Join-Path $root 'artifacts\win-x64-installer-source'
$installerScript = Join-Path $root 'installer\GameMacro.iss'
$installerOutput = Join-Path $root 'artifacts\installer\GameMacro-Setup.exe'

$env:DOTNET_CLI_HOME = Join-Path $root '.dotnet-home'
$env:NUGET_PACKAGES = Join-Path $root '.nuget\packages'
$env:APPDATA = Join-Path $root '.appdata'

dotnet publish $project `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false `
    -o $publishDirectory

if ($LASTEXITCODE -ne 0) {
    throw 'Application publish failed.'
}

$command = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
$candidates = @(
    if ($command) { $command.Source }
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe')
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
)
$iscc = $candidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
if (-not $iscc) {
    throw 'Inno Setup 6 was not found. Install it and run scripts\build-installer.ps1 again.'
}

& $iscc "/DMyAppVersion=$Version" $installerScript
if ($LASTEXITCODE -ne 0) {
    throw 'Installer compilation failed.'
}
if (-not (Test-Path -LiteralPath $installerOutput)) {
    throw "Installer output was not created: $installerOutput"
}

Get-Item -LiteralPath $installerOutput | Select-Object FullName, Length, LastWriteTime
