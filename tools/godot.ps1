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
pwsh -NoProfile -File tools/godot.ps1 -Check -Headless -Script res://../tools/verify_fixed_step_presentation.gd
pwsh -NoProfile -File tools/godot.ps1 --path game
#>
param(
    # Print the resolved executable and version, then exit.
    [switch]$Which,
    # Headless run of the main scene for -Frames frames; fails on engine errors.
    [switch]$Smoke,
    [int]$Frames = 120,
    # Headless editor import pass (refreshes .godot/ and missing .uid files); fails on engine errors.
    [switch]$Import,
    # Scan an explicit verification run, including custom scripts and rendered captures.
    [switch]$Check,
    [ValidateRange(1, 1800)]
    [int]$TimeoutSeconds = 120,
    [switch]$Headless,
    [string]$Script,
    # A JSON string array avoids PowerShell treating Godot's standalone -- as a parameter.
    [string]$UserArgsJson,
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
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $Exe
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    if ($startInfo.PSObject.Properties.Name -contains "ArgumentList") {
        foreach ($argument in $Arguments) { $startInfo.ArgumentList.Add($argument) }
    }
    else {
        # Windows PowerShell 5.1 has no ArgumentList; quote for the native argv parser,
        # never for a shell. Double backslashes before quotes and at the closing quote.
        $quoted = foreach ($argument in $Arguments) {
            $escaped = [regex]::Replace($argument, '(\\*)"', '$1$1\"')
            $escaped = [regex]::Replace($escaped, '(\\+)$', '$1$1')
            '"' + $escaped + '"'
        }
        $startInfo.Arguments = $quoted -join " "
    }
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    $timedOut = $false
    $clock = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        [void]$process.Start()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            $timedOut = $true
            if (-not $process.HasExited) {
                if ($PSVersionTable.PSEdition -eq "Core") { $process.Kill($true) }
                else { $process.Kill() }
            }
            [void]$process.WaitForExit(5000)
        }
        $exitCode = if ($process.HasExited) { $process.ExitCode } else { -1 }
        # An inherited pipe can outlive its parent. Never wait indefinitely for
        # ReadToEndAsync, even when the direct executable has already exited.
        $drainBudget = [Math]::Max(1, $TimeoutSeconds * 1000 - [int]$clock.ElapsedMilliseconds)
        $outputComplete = [System.Threading.Tasks.Task]::WaitAll(
            [System.Threading.Tasks.Task[]]@($stdout, $stderr), $drainBudget)
        if (-not $outputComplete) { $timedOut = $true }
        $outputs = @()
        if ($stdout.IsCompleted -and -not $stdout.IsFaulted) { $outputs += $stdout.Result }
        if ($stderr.IsCompleted -and -not $stderr.IsFaulted) { $outputs += $stderr.Result }
        foreach ($output in $outputs) {
            foreach ($rawLine in ($output -split "\r?\n")) {
                if ($rawLine.Length -eq 0) { continue }
                $line = $rawLine -replace "$esc\[[0-9;]*m", ""
                $lines.Add($line)
                Write-Host $line
            }
        }
    }
    finally {
        $process.Dispose()
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

    if ($timedOut -or $exitCode -ne 0 -or $hits.Count -gt 0) {
        Write-Host ""
        if ($timedOut) { Write-Host "Timed out after $TimeoutSeconds seconds." }
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
    $passThrough = @($GodotArgs)
    # The first delimiter belongs to PowerShell pass-through; subsequent ones belong
    # to Godot and must reach OS.get_cmdline_user_args() unchanged.
    if ($passThrough[0] -eq "--") { $passThrough = @($passThrough | Select-Object -Skip 1) }
}
if (-not ($passThrough -contains "--path")) {
    $passThrough = @("--path", $GameDir) + $passThrough
}
if ($Headless) { $passThrough += "--headless" }
if ($Script) { $passThrough += @("--script", $Script) }
if ($UserArgsJson) {
    $parsedArgs = ConvertFrom-Json -InputObject $UserArgsJson
    if (-not $UserArgsJson.TrimStart().StartsWith("[")) { throw "UserArgsJson must be a JSON string array." }
    $userArgs = @($parsedArgs)
    foreach ($argument in $userArgs) {
        if ($argument -isnot [string]) { throw "UserArgsJson must contain only strings." }
    }
    $passThrough += @("--") + $userArgs
}
if ($Check) {
    Invoke-ScannedGodot -Exe $godot.Exe -Label "verification" -Arguments $passThrough
}
& $godot.Exe @passThrough
exit $LASTEXITCODE
