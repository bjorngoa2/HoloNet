#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs HoloNet's Firefox enterprise policies (force-installs uBlock Origin) on the TV PC.
.DESCRIPTION
    Copies policies.json into Firefox's "distribution" folder, which applies it to every
    Firefox profile - including TvLauncher's dedicated kiosk profile. Re-run after editing
    policies.json. Restart Firefox afterwards; verify via about:policies.
#>
param(
    [string]$FirefoxInstallDir = "$env:ProgramFiles\Mozilla Firefox"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path (Join-Path $FirefoxInstallDir 'firefox.exe'))) {
    throw "firefox.exe not found in '$FirefoxInstallDir'. Pass -FirefoxInstallDir."
}

$distributionDir = Join-Path $FirefoxInstallDir 'distribution'
New-Item -ItemType Directory -Force -Path $distributionDir | Out-Null
Copy-Item -Path (Join-Path $PSScriptRoot 'policies.json') -Destination $distributionDir -Force

Write-Host "Installed policies.json to $distributionDir. Restart Firefox, then check about:policies."
