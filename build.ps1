# build.ps1 — Build MorePlayers and copy to BepInEx/plugins

param(
    [string]$GameDir = "I:\Bopl-Battle-AnkerGames\Bopl Battle\BoplBattle_Data\Managed",
    [string]$BepInExDir = "I:\Bopl-Battle-AnkerGames\Bopl Battle\BepInEx",
    [string]$Configuration = "Release"
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SrcDir = Join-Path $ScriptDir "src"
$PluginsDir = Join-Path $BepInExDir "plugins"

# Verify .NET SDK is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet SDK not found. Install from https://dotnet.microsoft.com/download/dotnet/4.8"
    exit 1
}

# Verify game DLLs exist
if (-not (Test-Path (Join-Path $GameDir "Assembly-CSharp.dll"))) {
    Write-Error "Assembly-CSharp.dll not found at $GameDir — check GameDir path"
    exit 1
}

Write-Host "Building MorePlayers..."
Write-Host "  GameDir    = $GameDir"
Write-Host "  BepInExDir = $BepInExDir"

Push-Location $SrcDir
try {
    dotnet build MorePlayers.csproj `
        -c $Configuration `
        /p:GameDir="$GameDir" `
        /p:BepInExDir="$BepInExDir" `
        --nologo

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    $OutputDll = Get-ChildItem -Path "bin\$Configuration\net46" -Filter "MorePlayers.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $OutputDll) {
        Write-Error "Build output MorePlayers.dll not found in bin\$Configuration\net46"
        exit 1
    }

    if (-not (Test-Path $PluginsDir)) {
        New-Item -ItemType Directory -Path $PluginsDir | Out-Null
    }

    Copy-Item -Path $OutputDll.FullName -Destination $PluginsDir -Force
    Write-Host ""
    Write-Host "Done. MorePlayers.dll copied to $PluginsDir"
}
finally {
    Pop-Location
}
