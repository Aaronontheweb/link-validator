# Hermetic smoke test for install.ps1. A local fake GitHub Releases API and
# stand-in archive exercise download, extraction, installation, and execution.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$Installer = Join-Path $RepoRoot "install.ps1"
$Version = "v0.0.0-smoke"
$Work = Join-Path ([System.IO.Path]::GetTempPath()) ("link-validator-install-smoke-" + [Guid]::NewGuid().ToString("N"))
$Serve = Join-Path $Work "serve"
$DownloadDir = Join-Path $Serve "downloads/$Version"
$Server = $null
$OriginalApiUrl = $env:LINK_VALIDATOR_GITHUB_API_URL
$CandidateArchive = $env:LINK_VALIDATOR_SMOKE_ARCHIVE

try {
    New-Item -ItemType Directory -Path $DownloadDir -Force | Out-Null

    $archiveName = "link-validator-windows-x64.zip"
    $archivePath = Join-Path $DownloadDir $archiveName

    if ($CandidateArchive) {
        if (-not (Test-Path $CandidateArchive)) {
            throw "Candidate archive was not found at $CandidateArchive"
        }
        if ((Split-Path -Leaf $CandidateArchive) -cne $archiveName) {
            throw "Candidate archive must be named $archiveName"
        }
        Copy-Item -Path $CandidateArchive -Destination $archivePath
    } else {
        $standIn = Join-Path $Work "link-validator.exe"
        $sourcePath = Join-Path $Work "Program.cs"
        $source = @'
using System;
public static class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("link-validator win-x64 smoke");
        return 0;
    }
}
'@
        [IO.File]::WriteAllText($sourcePath, $source)
        $compiler = Join-Path $env:WINDIR "Microsoft.NET/Framework64/v4.0.30319/csc.exe"
        if (-not (Test-Path $compiler)) {
            throw ".NET Framework C# compiler was not found at $compiler"
        }
        & $compiler /nologo /target:exe "/out:$standIn" $sourcePath
        if ($LASTEXITCODE -ne 0) { throw "Failed to compile the stand-in executable" }
        Compress-Archive -Path $standIn -DestinationPath $archivePath
    }

    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $port = ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    $listener.Stop()
    $baseUrl = "http://127.0.0.1:$port"

    $release = [ordered]@{
        tag_name = $Version
        assets = @([ordered]@{
            name = $archiveName
            browser_download_url = "$baseUrl/downloads/$Version/$archiveName"
        })
    } | ConvertTo-Json -Depth 4 -Compress

    $latestDir = Join-Path $Serve "releases"
    $tagsDir = Join-Path $latestDir "tags"
    New-Item -ItemType Directory -Path $tagsDir -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $latestDir "latest"), $release)
    [IO.File]::WriteAllText((Join-Path $tagsDir $Version), $release)

    $python = Get-Command python3 -ErrorAction SilentlyContinue
    if (-not $python) { $python = Get-Command python -ErrorAction Stop }
    $serverScript = Join-Path $PSScriptRoot "release-server.py"
    $Server = Start-Process -FilePath $python.Source -PassThru -NoNewWindow `
        -ArgumentList @($serverScript, $Serve, "$port") `
        -RedirectStandardOutput (Join-Path $Work "http.out") `
        -RedirectStandardError (Join-Path $Work "http.err")

    $ready = $false
    for ($i = 0; $i -lt 50; $i++) {
        try {
            Invoke-WebRequest "$baseUrl/releases/latest" -UseBasicParsing -TimeoutSec 2 | Out-Null
            $ready = $true
            break
        } catch {
            Start-Sleep -Milliseconds 100
        }
    }
    if (-not $ready) { throw "Local release server failed to start" }

    $env:LINK_VALIDATOR_GITHUB_API_URL = $baseUrl
    $installDir = Join-Path $Work "installed"
    & $Installer -InstallPath $installDir -SkipPath

    $installedBinary = Join-Path $installDir "link-validator.exe"
    if (-not (Test-Path $installedBinary)) {
        throw "Installed binary was not found at $installedBinary"
    }
    $output = & $installedBinary --version | Out-String
    if ($LASTEXITCODE -ne 0 -or (-not $CandidateArchive -and $output -notmatch "link-validator win-x64 smoke")) {
        throw "Installed stand-in did not execute successfully"
    }

    Write-Host "PASS: Windows x64 latest release installed and executed"
} finally {
    $env:LINK_VALIDATOR_GITHUB_API_URL = $OriginalApiUrl
    if ($null -ne $Server -and -not $Server.HasExited) {
        Stop-Process -Id $Server.Id -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path $Work) {
        Remove-Item -Path $Work -Recurse -Force -ErrorAction SilentlyContinue
    }
}
