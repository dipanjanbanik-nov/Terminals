<#
.SYNOPSIS
    Cleans the Terminals.sln solution and removes all bin/obj folders under the Source directory.

.DESCRIPTION
    1. Locates MSBuild (via vswhere, falling back to common install paths) and runs "msbuild Terminals.sln /t:Clean"
       for both Debug and Release configurations.
    2. Recursively finds and deletes every "bin" and "obj" folder under the Source directory.
#>

$ErrorActionPreference = "Stop"

$SourcePath = Join-Path (Split-Path $PSScriptRoot -Parent) "Source"
$SolutionPath = Join-Path $SourcePath "Terminals.sln"

# Folders that should never be scanned/cleaned by this script.
$ExcludedPaths = @(
    (Join-Path $SourcePath "Terminals\Unified")
)

if (-not (Test-Path $SourcePath)) {
    Write-Error "Source path not found: $SourcePath"
    exit 1
}

function Test-IsExcludedPath {
    param([string]$Path)

    foreach ($excluded in $ExcludedPaths) {
        if ($Path -eq $excluded -or $Path.StartsWith("$excluded\", [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function Find-MSBuild {
    # Try vswhere first (installed with Visual Studio 2017+)
    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $installPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2>$null
        if ($installPath) {
            $candidates = @(
                Join-Path $installPath "MSBuild\Current\Bin\MSBuild.exe"
                Join-Path $installPath "MSBuild\15.0\Bin\MSBuild.exe"
            )
            foreach ($candidate in $candidates) {
                if (Test-Path $candidate) {
                    return $candidate
                }
            }
        }
    }

    # Fallback: check PATH
    $onPath = Get-Command "msbuild.exe" -ErrorAction SilentlyContinue
    if ($onPath) {
        return $onPath.Source
    }

    # Fallback: common .NET Framework install locations
    $frameworkCandidates = @(
        "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
        "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
    )
    foreach ($candidate in $frameworkCandidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    return $null
}

if (-not (Test-Path $SolutionPath)) {
    Write-Warning "Solution not found at '$SolutionPath'. Skipping solution clean."
}
else {
    $msbuildPath = Find-MSBuild
    if (-not $msbuildPath) {
        Write-Warning "MSBuild.exe could not be located. Skipping solution clean step."
    }
    else {
        Write-Host "Using MSBuild: $msbuildPath" -ForegroundColor Cyan

        foreach ($configuration in @("Debug", "Release")) {
            Write-Host "Cleaning solution '$SolutionPath' (Configuration=$configuration)..." -ForegroundColor Cyan
            & $msbuildPath $SolutionPath /t:Clean "/p:Configuration=$configuration" /v:minimal /nologo
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "MSBuild clean for configuration '$configuration' exited with code $LASTEXITCODE."
            }
        }
    }
}

Write-Host "Searching for bin/obj folders under '$SourcePath'..." -ForegroundColor Cyan

foreach ($excluded in $ExcludedPaths) {
    if (Test-Path $excluded) {
        Write-Host "Excluding folder from scan: $excluded" -ForegroundColor DarkYellow
    }
}

$foldersToDelete = Get-ChildItem -Path $SourcePath -Directory -Recurse -Force -ErrorAction SilentlyContinue |
Where-Object { $_.Name -in @("bin", "obj") -and -not (Test-IsExcludedPath $_.FullName) }

if (-not $foldersToDelete) {
    Write-Host "No bin/obj folders found." -ForegroundColor Green
}
else {
    foreach ($folder in $foldersToDelete) {
        # Skip if already removed as part of a parent folder deletion.
        if (-not (Test-Path $folder.FullName)) {
            continue
        }

        Write-Host "Deleting $($folder.FullName)" -ForegroundColor Yellow
        try {
            Remove-Item -Path $folder.FullName -Recurse -Force -ErrorAction Stop
        }
        catch {
            Write-Warning "Failed to delete '$($folder.FullName)': $_"
        }
    }

    Write-Host "Done. Removed $($foldersToDelete.Count) bin/obj folder(s)." -ForegroundColor Green
}

Write-Host "Press any key to exit..." -ForegroundColor Cyan
[void][System.Console]::ReadKey($true)