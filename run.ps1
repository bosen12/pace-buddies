$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    & dotnet build src/PaceBuddies -c Release --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    Start-Process -FilePath (Join-Path $PSScriptRoot 'src/PaceBuddies/bin/Release/net8.0-windows/PaceBuddies.exe') -WindowStyle Hidden
} finally { Pop-Location }
