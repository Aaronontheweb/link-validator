#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Downloads and installs the latest release of LinkValidator
.DESCRIPTION
    This script downloads the appropriate LinkValidator binary for your platform
    and installs it to a specified directory.
.PARAMETER InstallPath
    Directory to install LinkValidator to. Defaults to $env:USERPROFILE\.linkvalidator on Windows
    or ~/.linkvalidator on Unix-like systems.
.PARAMETER Version
    Specific version to install. If not specified, installs the latest release.
.PARAMETER AddToPath
    Whether to add the install directory to PATH (requires admin/sudo on some systems)
.EXAMPLE
    .\install.ps1
    Installs the latest version to the default location
.EXAMPLE
    .\install.ps1 -InstallPath "C:\tools\linkvalidator" -AddToPath
    Installs to a custom path and adds to PATH
.EXAMPLE
    .\install.ps1 -Version "v1.0.0"
    Installs a specific version
#>

param(
    [string]$InstallPath,
    [string]$Version,
    [switch]$AddToPath
)

$ErrorActionPreference = 'Stop'

# Constants
$REPO_OWNER = "stannardlabs"
$REPO_NAME = "link-validator"
$GITHUB_API_URL = "https://api.github.com/repos/$REPO_OWNER/$REPO_NAME"

# Determine platform
if ($IsWindows -or $env:OS -eq "Windows_NT") {
    $Platform = "windows"
    $Architecture = "x64"
    $Extension = ".exe"
    $ArchiveExt = ".zip"
    $DefaultInstallPath = Join-Path $env:USERPROFILE ".linkvalidator"
} elseif ($IsLinux) {
    $Platform = "linux"
    $Architecture = "x64"
    $Extension = ""
    $ArchiveExt = ".tar.gz"
    $DefaultInstallPath = Join-Path $env:HOME ".linkvalidator"
} elseif ($IsMacOS) {
    # Detect Apple Silicon vs Intel
    $UnameM = uname -m
    if ($UnameM -eq "arm64") {
        $Architecture = "arm64"
    } else {
        $Architecture = "x64"
    }
    $Platform = "macos"
    $Extension = ""
    $ArchiveExt = ".tar.gz"
    $DefaultInstallPath = Join-Path $env:HOME ".linkvalidator"
} else {
    throw "Unsupported platform"
}

# Use default install path if not specified
if (-not $InstallPath) {
    $InstallPath = $DefaultInstallPath
}

Write-Host "LinkValidator Installer" -ForegroundColor Green
Write-Host "Platform: $Platform-$Architecture" -ForegroundColor Cyan
Write-Host "Install Path: $InstallPath" -ForegroundColor Cyan

# Get latest release info or specific version
if ($Version) {
    $ReleaseUrl = "$GITHUB_API_URL/releases/tags/$Version"
    Write-Host "Fetching version: $Version" -ForegroundColor Cyan
} else {
    $ReleaseUrl = "$GITHUB_API_URL/releases/latest"
    Write-Host "Fetching latest release..." -ForegroundColor Cyan
}

try {
    $Release = Invoke-RestMethod -Uri $ReleaseUrl -Headers @{ "User-Agent" = "LinkValidator-Installer" }
    $Version = $Release.tag_name
    Write-Host "Version: $Version" -ForegroundColor Green
} catch {
    throw "Failed to fetch release information: $_"
}

# Find the appropriate asset
$AssetName = "link-validator-$Platform-$Architecture$ArchiveExt"
$Asset = $Release.assets | Where-Object { $_.name -eq $AssetName }

if (-not $Asset) {
    Write-Host "Available assets:" -ForegroundColor Yellow
    $Release.assets | ForEach-Object { Write-Host "  - $($_.name)" -ForegroundColor Yellow }
    throw "Could not find asset for platform $Platform-$Architecture"
}

# Create install directory
Write-Host "Creating install directory..." -ForegroundColor Cyan
if (-not (Test-Path $InstallPath)) {
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
}

# Download and extract
$TempDir = Join-Path ([System.IO.Path]::GetTempPath()) "linkvalidator-$([System.Guid]::NewGuid().ToString())"
New-Item -ItemType Directory -Path $TempDir -Force | Out-Null

try {
    $ArchivePath = Join-Path $TempDir $Asset.name
    Write-Host "Downloading $AssetName..." -ForegroundColor Cyan
    
    Invoke-WebRequest -Uri $Asset.browser_download_url -OutFile $ArchivePath -Headers @{ "User-Agent" = "LinkValidator-Installer" }
    
    Write-Host "Extracting..." -ForegroundColor Cyan
    if ($ArchiveExt -eq ".zip") {
        Expand-Archive -Path $ArchivePath -DestinationPath $TempDir -Force
    } else {
        tar -xzf $ArchivePath -C $TempDir
    }
    
    # Move binary to install path
    $BinaryName = "LinkValidator$Extension"
    $SourcePath = Join-Path $TempDir $BinaryName
    $DestPath = Join-Path $InstallPath $BinaryName
    
    if (Test-Path $SourcePath) {
        Move-Item -Path $SourcePath -Destination $DestPath -Force
        
        # Make executable on Unix-like systems
        if ($Platform -ne "windows") {
            chmod +x $DestPath
        }
        
        Write-Host "✓ LinkValidator installed successfully!" -ForegroundColor Green
        Write-Host "Binary location: $DestPath" -ForegroundColor Cyan
        
        # Test installation
        Write-Host "Testing installation..." -ForegroundColor Cyan
        & $DestPath --version
        
        # Add to PATH if requested
        if ($AddToPath) {
            Write-Host "Adding to PATH..." -ForegroundColor Cyan
            
            if ($Platform -eq "windows") {
                # Windows: Add to user PATH
                $CurrentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
                if ($CurrentPath -notlike "*$InstallPath*") {
                    [Environment]::SetEnvironmentVariable("PATH", "$CurrentPath;$InstallPath", "User")
                    Write-Host "✓ Added to user PATH. Restart your terminal to use 'LinkValidator' command." -ForegroundColor Green
                } else {
                    Write-Host "✓ Already in PATH" -ForegroundColor Green
                }
            } else {
                # Unix-like: Add to shell profile
                $ShellProfile = if (Test-Path ~/.zshrc) { "~/.zshrc" } 
                               elseif (Test-Path ~/.bashrc) { "~/.bashrc" } 
                               else { "~/.profile" }
                
                $PathLine = "export PATH=`"$InstallPath:`$PATH`""
                
                if (-not (Get-Content $ShellProfile -ErrorAction SilentlyContinue | Select-String -Pattern [regex]::Escape($InstallPath))) {
                    Add-Content -Path $ShellProfile -Value $PathLine
                    Write-Host "✓ Added to $ShellProfile. Run 'source $ShellProfile' or restart your terminal." -ForegroundColor Green
                } else {
                    Write-Host "✓ Already in shell profile" -ForegroundColor Green
                }
            }
        } else {
            Write-Host "Run with -AddToPath to add to your PATH, or use the full path:" -ForegroundColor Yellow
            Write-Host "  $DestPath --url https://example.com" -ForegroundColor Yellow
        }
        
    } else {
        throw "Binary not found in archive"
    }
    
} finally {
    # Cleanup
    if (Test-Path $TempDir) {
        Remove-Item -Path $TempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}