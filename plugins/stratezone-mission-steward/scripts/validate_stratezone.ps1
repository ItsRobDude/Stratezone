param(
    [switch]$SkipGodot,
    [string]$GodotExe = $env:GODOT_EXE
)

$ErrorActionPreference = "Stop"
$script:Failures = @()
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")

function Invoke-StratezoneStep {
    param(
        [string]$Name,
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "== $Name =="
    $global:LASTEXITCODE = 0

    try {
        & $Command
        if ($LASTEXITCODE -ne 0) {
            throw "$Name exited with code $LASTEXITCODE"
        }
        Write-Host "PASS: $Name"
    }
    catch {
        Write-Host "FAIL: $Name"
        Write-Host $_.Exception.Message
        $script:Failures += $Name
    }
}

function Resolve-StratezoneGodot {
    param([string]$Candidate)

    if ($Candidate -and (Test-Path -LiteralPath $Candidate)) {
        return (Resolve-Path -LiteralPath $Candidate).Path
    }

    $commands = @(
        "Godot_v4.6.2-stable_mono_win64_console.exe",
        "Godot_v4.6-stable_mono_win64_console.exe",
        "godot",
        "godot4"
    )

    foreach ($command in $commands) {
        $found = Get-Command $command -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            return $found.Source
        }
    }

    return $null
}

Push-Location $RepoRoot
try {
    Invoke-StratezoneStep "Content validation" {
        python tools\validate_content.py
    }

    Invoke-StratezoneStep "Godot C# build" {
        dotnet build game\Stratezone.csproj
    }

    Invoke-StratezoneStep "Simulation smoke" {
        dotnet run --project tests\SimulationSmoke\SimulationSmoke.csproj
    }

    Invoke-StratezoneStep "Content drift check" {
        python plugins\stratezone-mission-steward\scripts\check_content_drift.py
    }

    if (-not $SkipGodot) {
        $resolvedGodot = Resolve-StratezoneGodot -Candidate $GodotExe
        if (-not $resolvedGodot) {
            Write-Host ""
            Write-Host "FAIL: Godot headless smoke"
            Write-Host "Godot console executable was not found. Set GODOT_EXE or pass -GodotExe."
            $script:Failures += "Godot headless smoke"
        }
        else {
            Invoke-StratezoneStep "Godot headless smoke" {
                & $resolvedGodot --headless --path game --quit
            }
        }
    }
}
finally {
    Pop-Location
}

Write-Host ""
if ($script:Failures.Count -gt 0) {
    Write-Host "Stratezone validation failed:"
    foreach ($failure in $script:Failures) {
        Write-Host "- $failure"
    }
    exit 1
}

Write-Host "Stratezone validation passed."
exit 0

