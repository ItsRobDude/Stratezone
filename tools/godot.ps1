<#
.SYNOPSIS
Single entry point for launching Godot against the Stratezone project.

.DESCRIPTION
Resolves the Godot .NET (Mono) console executable whose version matches the
Godot.NET.Sdk pin in game/Stratezone.csproj, and refuses anything else.
A non-Mono Godot prints "No loader found for resource: ...cs" but still exits 0,
so headless modes here also scan the output for engine errors and fail loudly.

Resolution: $env:GODOT_EXE when set (authoritative), otherwise WinGet Mono packages,
then godot/godot4 on PATH. The chosen build must report ".mono." and match the pin.

.EXAMPLE
pwsh -NoProfile -File tools/godot.ps1 -Which
pwsh -NoProfile -File tools/godot.ps1 -Smoke
pwsh -NoProfile -File tools/godot.ps1 -Import
pwsh -NoProfile -File tools/godot.ps1 -- --path game
#>
param(
    # Print the resolved executable and version, then exit.
    [switch]$Which,
    # Headless run of the main scene for -Frames frames; fails on engine errors.
    [switch]$Smoke,
    [int]$Frames = 120,
    # Headless editor import pass (refreshes .godot/ and missing .uid files); fails on engine errors.
    [switch]$Import,
    # Anything else is passed straight to Godot. --path game is added when no --path is given.
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$GodotArgs
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$GameDir = Join-Path $RepoRoot "game"
$Csproj = Join-Path $GameDir "Stratezone.csproj"

# Patterns that mean the engine or C# layer failed even when the exit code is 0.
$ErrorPatterns = @(
    "No loader found for resource",
    "SCRIPT ERROR",
    "USER ERROR",
    "^\s*ERROR:",
    "Unhandled exception",
    "System\.[A-Za-z.]*Exception"
)

function Get-PinnedVersion {
    $text = Get-Content -LiteralPath $Csproj -Raw
    $match = [regex]::Match($text, "Godot\.NET\.Sdk/(\d+)\.(\d+)(?:\.(\d+))?")
    if (-not $match.Success) {
        throw "Could not read the Godot.NET.Sdk version from $Csproj."
    }
    $major = $match.Groups[1].Value
    $minor = $match.Groups[2].Value
    $patch = $match.Groups[3].Value
    # Godot reports x.y.0 releases as "x.y.stable" and patch releases as "x.y.z.stable".
    if ($patch -and $patch -ne "0") {
        return "$major.$minor.$patch"
    }
    return "$major.$minor"
}

function Get-GodotVersion {
    param([string]$Exe)
    try {
        $output = & $Exe --version 2>$null | Select-Object -First 1
        return ("$output").Trim()
    }
    catch {
        return ""
    }
}

function Get-Candidates {
    $candidates = New-Object System.Collections.Generic.List[string]
    if ($env:GODOT_EXE) {
        # An explicit override is authoritative: it must match or the run fails.
        $candidates.Add($env:GODOT_EXE)
        return $candidates
    }
    if ($env:LOCALAPPDATA) {
        $wingetRoot = Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages"
        if (Test-Path -LiteralPath $wingetRoot) {
            Get-ChildItem -Path $wingetRoot -Directory -Filter "GodotEngine.GodotEngine.Mono_*" -ErrorAction SilentlyContinue |
                ForEach-Object {
                    Get-ChildItem -Path $_.FullName -Recurse -Filter "Godot_v*_mono_*_console.exe" -ErrorAction SilentlyContinue
                } |
                Sort-Object FullName -Descending |
                ForEach-Object { $candidates.Add($_.FullName) }
        }
    }
    foreach ($name in @("godot", "godot4")) {
        $found = Get-Command $name -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) {
            $candidates.Add($found.Source)
        }
    }
    return $candidates
}

function Resolve-Godot {
    $pinned = Get-PinnedVersion
    $rejected = New-Object System.Collections.Generic.List[string]
    foreach ($candidate in (Get-Candidates)) {
        if (-not (Test-Path -LiteralPath $candidate)) {
            $rejected.Add("$candidate (missing)")
            continue
        }
        $version = Get-GodotVersion -Exe $candidate
        $isMono = $version -match "\.mono\."
        $matchesPin = $version.StartsWith("$pinned.stable")
        if ($isMono -and $matchesPin) {
            return [pscustomobject]@{ Exe = $candidate; Version = $version; Pinned = $pinned }
        }
        $why = @()
        if (-not $isMono) { $why += "not a .NET/Mono build" }
        if (-not $matchesPin) { $why += "version is not $pinned" }
        $rejected.Add("$candidate -> '$version' ($($why -join ', '))")
    }

    Write-Host "No Godot .NET build matching Godot.NET.Sdk $pinned was found."
    if ($rejected.Count -gt 0) {
        Write-Host "Rejected candidates:"
        $rejected | ForEach-Object { Write-Host "  - $_" }
    }
    Write-Host "Install the matching Godot .NET build or set GODOT_EXE to its *_console.exe."
    exit 2
}

function Invoke-ScannedGodot {
    param(
        [string]$Exe,
        [string[]]$Arguments,
        [string]$Label
    )
    $esc = [char]27
    $lines = New-Object System.Collections.Generic.List[string]
    $previousPreference = $ErrorActionPreference
    # Windows PowerShell turns native stderr into error records; keep them as plain text.
    $ErrorActionPreference = "Continue"
    try {
        & $Exe @Arguments 2>&1 | ForEach-Object {
            $line = ("$_") -replace "$esc\[[0-9;]*m", ""
            $lines.Add($line)
            Write-Host $line
        }
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }

    $hits = @()
    foreach ($line in $lines) {
        foreach ($pattern in $ErrorPatterns) {
            if ($line -match $pattern) {
                $hits += $line.Trim()
                break
            }
        }
    }

    if ($exitCode -ne 0 -or $hits.Count -gt 0) {
        Write-Host ""
        Write-Host "FAIL: Godot $Label (exit code $exitCode, $($hits.Count) error line(s))."
        $hits | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" }
        exit 1
    }
    Write-Host ""
    Write-Host "PASS: Godot $Label."
    exit 0
}

$godot = Resolve-Godot

if ($Which) {
    Write-Host "Godot: $($godot.Exe)"
    Write-Host "Version: $($godot.Version) (pin: Godot.NET.Sdk $($godot.Pinned))"
    exit 0
}

if ($Smoke) {
    Invoke-ScannedGodot -Exe $godot.Exe -Label "headless smoke ($Frames frames)" -Arguments @(
        "--headless", "--path", $GameDir, "--quit-after", "$Frames"
    )
}

if ($Import) {
    Invoke-ScannedGodot -Exe $godot.Exe -Label "headless import" -Arguments @(
        "--headless", "--path", $GameDir, "--import"
    )
}

$passThrough = @()
if ($GodotArgs) {
    $passThrough = @($GodotArgs | Where-Object { $_ -ne "--" })
}
if (-not ($passThrough -contains "--path")) {
    $passThrough = @("--path", $GameDir) + $passThrough
}
& $godot.Exe @passThrough
exit $LASTEXITCODE
