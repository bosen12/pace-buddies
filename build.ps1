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
        $paceVersion = ([xml](Get-Content -LiteralPath src/PaceBuddies/PaceBuddies.csproj -Raw)).Project.PropertyGroup.Version
        $paceOutput = "dist/PaceBuddies-$paceVersion-win-x64"
        & dotnet publish src/PaceBuddies -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None --source https://api.nuget.org/v3/index.json -o $paceOutput --disable-build-servers
        if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
        Copy-Item -LiteralPath README.md -Destination "$paceOutput/README.md"
        Compress-Archive -LiteralPath $paceOutput -DestinationPath "dist/PaceBuddies-$paceVersion-win-x64.zip" -Force
    }
} finally { Pop-Location }
