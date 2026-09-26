<#
.SYNOPSIS
    Builds the unpackaged (non-MSIX) win-x64 build of V0X Media Encode and packs it into a
    Velopack release for direct-download auto-updates (separate channel from the MSIX build -
    Velopack manages its own installed layout, which the MSIX/AppX sandbox does not allow).

.DESCRIPTION
    Requires the `vpk` dotnet tool: dotnet tool install -g vpk
    Requires a version number for this release (semver, e.g. 1.0.0) - Velopack uses it to compute
    delta updates against the previous release in -OutputDirectory.

.PARAMETER Version
    Semver for this release, e.g. "1.0.0".
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "Releases"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    throw "vpk CLI not found. Install it with: dotnet tool install -g vpk"
}

$repoRoot = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $repoRoot "src\V0XMediaEncode.App\V0XMediaEncode.App.csproj"
$publishDir = Join-Path $repoRoot "publish\win-x64"

Write-Host "Publishing unpackaged win-x64 build ($Configuration)..."
dotnet publish $projectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained `
    -p:WindowsPackageType=None `
    -p:WindowsAppSDKSelfContained=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Write-Host "Packing Velopack release v$Version..."
vpk pack `
    --packId V0XMediaEncode `
    --packVersion $Version `
    --packDir $publishDir `
    --mainExe V0XMediaEncode.App.exe `
    --outputDir (Join-Path $repoRoot $OutputDirectory)

if ($LASTEXITCODE -ne 0) {
    throw "vpk pack failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "Release artifacts written under $OutputDirectory\ - upload them to a new GitHub release"
Write-Host "(tag v$Version) on the repo UpdateService.GithubSource points at."
