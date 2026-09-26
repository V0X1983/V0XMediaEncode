<#
.SYNOPSIS
    Generates a self-signed code-signing certificate for local MSIX sideloading of V0X Media
    Encode, matching the Publisher identity declared in Package.appxmanifest (CN=V0X).

.DESCRIPTION
    Not for Store submission or public distribution - anyone installing the resulting MSIX must
    first trust this certificate (see the Import-PfxCertificate command this script prints).
    The .pfx and its password are written under certs/, which is gitignored; re-run this script
    to rotate the certificate.
#>
[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\certs"),
    [string]$Subject = "CN=V0X"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

$pfxPath = Join-Path $OutputDirectory "V0XMediaEncode_DevCert.pfx"
$passwordPath = Join-Path $OutputDirectory "dev-cert-password.txt"

$randomBytes = New-Object byte[] 24
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try {
    $rng.GetBytes($randomBytes)
} finally {
    $rng.Dispose()
}
$password = [Convert]::ToBase64String($randomBytes) -replace '[+/=]', ''
$securePassword = ConvertTo-SecureString -String $password -Force -AsPlainText

$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $Subject `
    -KeyUsage DigitalSignature `
    -FriendlyName "V0X Media Encode Dev Certificate" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")


# TripleDES_SHA1 (the legacy PFX encoding), not the AES256/SHA256 default on modern Windows -
# the MSIX packaging tooling's certificate import (WinAppSdkValidateSigningCertificate) is built
# on older crypto APIs that fail to read the modern default with a misleading "wrong password"
# error even when the password is correct.
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePassword -CryptoAlgorithmOption TripleDES_SHA1 | Out-Null
Set-Content -Path $passwordPath -Value $password -NoNewline

Write-Host "Dev certificate created:"
Write-Host "  Thumbprint : $($cert.Thumbprint)"
Write-Host "  PFX        : $pfxPath"
Write-Host "  Password   : $passwordPath (gitignored)"
Write-Host ""
Write-Host "To trust it on THIS machine so the signed MSIX installs without warnings, run as Administrator:"
Write-Host "  Import-PfxCertificate -FilePath `"$pfxPath`" -CertStoreLocation Cert:\LocalMachine\TrustedPeople -Password (ConvertTo-SecureString -String (Get-Content `"$passwordPath`" -Raw) -Force -AsPlainText)"
Write-Host ""
Write-Host "Then build the signed package with: scripts\Publish-Msix.ps1"
