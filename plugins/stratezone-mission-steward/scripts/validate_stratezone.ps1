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
        # tools/godot.ps1 resolves the Mono build matching the csproj pin and fails on
        # engine errors in the log, not just on the exit code.
        if ($GodotExe) {
            $env:GODOT_EXE = $GodotExe
        }
        Invoke-StratezoneStep "Godot headless smoke" {
            & (Join-Path $RepoRoot "tools\godot.ps1") -Smoke
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

