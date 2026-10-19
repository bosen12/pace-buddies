param([switch]$Publish)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    & dotnet run --project tests/PaceBuddies.Checks -c Release --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw 'Core checks failed.' }
    & dotnet build src/PaceBuddies -c Release --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    if ($Publish) {
        & dotnet publish src/PaceBuddies -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None --source https://api.nuget.org/v3/index.json -o dist/PaceBuddies-win-x64 --disable-build-servers
        if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
        Copy-Item -LiteralPath README.md -Destination dist/PaceBuddies-win-x64/README.md
        Compress-Archive -LiteralPath dist/PaceBuddies-win-x64 -DestinationPath dist/PaceBuddies-0.1.0-win-x64.zip -Force
    }
} finally { Pop-Location }
