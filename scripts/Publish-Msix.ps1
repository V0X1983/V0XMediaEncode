<#
.SYNOPSIS
    Builds a signed, sideloadable MSIX package for V0X Media Encode using the local dev
    certificate (see New-DevCertificate.ps1).

.PARAMETER Platform
    x64 or ARM64.
#>
[CmdletBinding()]
param(
    [ValidateSet("x64", "ARM64")]
    [string]$Platform = "x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent
$certPath = Join-Path $repoRoot "certs\V0XMediaEncode_DevCert.pfx"
$passwordPath = Join-Path $repoRoot "certs\dev-cert-password.txt"
$projectPath = Join-Path $repoRoot "src\V0XMediaEncode.App\V0XMediaEncode.App.csproj"

if (-not (Test-Path $certPath) -or -not (Test-Path $passwordPath)) {
    Write-Host "No dev certificate found - generating one first..."
    & (Join-Path $PSScriptRoot "New-DevCertificate.ps1")
}

$certPassword = Get-Content $passwordPath -Raw

Write-Host "Publishing signed MSIX ($Platform, $Configuration)..."
dotnet publish $projectPath `
    -c $Configuration `
    -p:Platform=$Platform `
    -p:RuntimeIdentifier="win-$($Platform.ToLower())" `
    -p:GenerateAppxPackageOnBuild=true `
    -p:AppxPackageSigningEnabled=true `
    -p:PackageCertificateKeyFile=$certPath `
    -p:PackageCertificatePassword=$certPassword `
    -p:UapAppxPackageBuildMode=SideloadOnly

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Write-Host ""
Write-Host "Signed MSIX package(s) written under src\V0XMediaEncode.App\AppPackages\"
Write-Host "Install with: Add-AppxPackage -Path <path-to-.msix>"
Write-Host "(the machine must first trust the dev cert - see New-DevCertificate.ps1's output)"
