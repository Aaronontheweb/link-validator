# LinkValidator

[![License](https://img.shields.io/github/license/Aaronontheweb/link-validator)](LICENSE)
[![GitHub release](https://img.shields.io/github/release/Aaronontheweb/link-validator.svg)](https://github.com/Aaronontheweb/link-validator/releases)
[![Build Status](https://img.shields.io/github/actions/workflow/status/Aaronontheweb/link-validator/release.yml)](https://github.com/Aaronontheweb/link-validator/actions)

A fast, reliable CLI tool for crawling websites and validating both internal and external links. Built with [Akka.NET](https://getakka.net/) for high-performance concurrent crawling.

## ✨ Features

- **Fast Concurrent Crawling** - Leverages Akka.NET actors for efficient parallel processing
- **Smart External Link Handling** - Respects rate limits with configurable retry policies for 429 responses
- **Comprehensive Reporting** - Generate detailed markdown reports of all discovered links and their status
- **CI/CD Ready** - Perfect for automated testing in build pipelines
- **Cross-Platform** - Single-file binaries for Windows, Linux, and macOS (Intel + Apple Silicon)
- **Diff Support** - Compare current crawl results against previous runs to detect changes
- **Flexible Configuration** - CLI flags and environment variables for easy customization

## 🚀 Quick Start

**Crawl a website:**
```bash
link-validator --url https://example.com
```

**Save results and enable strict mode for CI:**
```bash
link-validator --url https://example.com --output sitemap.md --strict
```

**Compare against previous results:**
```bash
link-validator --url https://example.com --output new-sitemap.md --diff old-sitemap.md --strict
```

## 📦 Installation

### Option 1: Install Script (Recommended)

**Windows (PowerShell):**
```powershell
# Install to default location and add to PATH
irm https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.ps1 | iex

# Install to custom location  
irm https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.ps1 | iex -ArgumentList "-InstallPath", "C:\tools\linkvalidator"

# Install without adding to PATH
irm https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.ps1 | iex -ArgumentList "-SkipPath"
```

**Linux/macOS (Bash):**
```bash
# Install to default location and add to PATH
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash

# Install to custom location
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --dir ~/.local/bin

# Install without adding to PATH
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --skip-path
```

### Option 2: Download Binary

Download the appropriate binary from the [latest release](https://github.com/Aaronontheweb/link-validator/releases/latest):

- **Windows x64:** `link-validator-windows-x64.zip`
- **Linux x64:** `link-validator-linux-x64.tar.gz` 
- **macOS x64:** `link-validator-macos-x64.tar.gz`
- **macOS ARM64:** `link-validator-macos-arm64.tar.gz`

Extract and place the binary in your PATH.

### Option 3: Build from Source

**Prerequisites:** [.NET 9 SDK](https://dotnet.microsoft.com/download)

```bash
git clone https://github.com/Aaronontheweb/link-validator.git
cd link-validator

# Build and run locally
dotnet run --project src/LinkValidator -- --url https://example.com

# Or publish as single-file binary
dotnet publish src/LinkValidator -c Release -r <RUNTIME> --self-contained false
# Where <RUNTIME> is: win-x64, linux-x64, osx-x64, or osx-arm64
```

## 🏗️ CI/CD Integration

LinkValidator is designed to integrate seamlessly into your build pipelines to catch broken links before they reach production.

### GitHub Actions

```yaml
name: Link Validation

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  validate-links:
    runs-on: ubuntu-latest
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      
    - name: Install LinkValidator
      run: |
        curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --add-to-path
        
    - name: Validate Links
      run: |
        # Deploy your site locally first (example with Jekyll)
        bundle exec jekyll build
        bundle exec jekyll serve --detach
        
        # Wait for server to start
        sleep 5
        
        # Validate links in strict mode
        LinkValidator --url http://localhost:4000 --strict
        
    - name: Upload sitemap artifact
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: sitemap
        path: sitemap.md
```

### Azure DevOps

```yaml
trigger:
  branches:
    include:
    - main
    - develop

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: Bash@3
  displayName: 'Install LinkValidator'
  inputs:
    targetType: 'inline'
    script: |
      curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --add-to-path

- task: Bash@3
  displayName: 'Validate Links'
  inputs:
    targetType: 'inline'
    script: |
      # Start your local server
      npm run build
      npm run serve &
      sleep 5
      
      # Validate with comparison against baseline
      LinkValidator --url http://localhost:3000 --output current-sitemap.md --diff baseline-sitemap.md --strict
```

### Jenkins

```groovy
pipeline {
    agent any
    
    stages {
        stage('Install LinkValidator') {
            steps {
                sh '''
                    curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --dir ./.linkvalidator
                '''
            }
        }
        
        stage('Build Site') {
            steps {
                sh '''
                    # Build your static site
                    npm run build
                    npm run serve &
                    sleep 5
                '''
            }
        }
        
        stage('Validate Links') {
            steps {
                sh '''
                    ./.linkvalidator/LinkValidator --url http://localhost:3000 --strict --output sitemap.md
                '''
            }
            post {
                always {
                    archiveArtifacts artifacts: 'sitemap.md', fingerprint: true
                }
            }
        }
    }
}
```

### Docker Integration

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS base
WORKDIR /app

# Install LinkValidator
RUN apk add --no-cache curl bash && \
    curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/main/install.sh | bash -s -- --dir /usr/local/bin

# Your application setup here...

# Validate links as part of health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD LinkValidator --url http://localhost:80 --max-external-retries 1 --retry-delay-seconds 5
```

## 🔧 Usage

### Basic Usage

```bash
link-validator --url <URL> [OPTIONS]
```

### Command Line Options

| Option | Description | Default |
|--------|-------------|---------|
| `--url <URL>` | **Required.** The URL to crawl | - |
| `--output <PATH>` | Save sitemap report to file | Print to stdout |
| `--diff <PATH>` | Compare against previous sitemap file | - |
| `--strict` | Return error code if broken links found | `false` |
| `--max-external-retries <N>` | Max retries for external 429 responses | `3` |
| `--retry-delay-seconds <N>` | Default retry delay (when no Retry-After header) | `10` |
| `--help` | Show help information | - |
| `--version` | Show version information | - |

### Environment Variables

Override default values using environment variables:

```bash
export LINK_VALIDATOR_MAX_EXTERNAL_RETRIES=5
export LINK_VALIDATOR_RETRY_DELAY_SECONDS=15
link-validator --url https://example.com
```

### Examples

**Basic website crawl:**
```bash
link-validator --url https://aaronstannard.com
```

**Save results to file:**
```bash
link-validator --url https://aaronstannard.com --output sitemap.md
```

**Strict mode for CI (fails on broken links):**
```bash
link-validator --url https://aaronstannard.com --strict
```

**Compare with previous crawl:**
```bash
# First crawl
link-validator --url https://aaronstannard.com --output baseline.md

# Later crawl with comparison
link-validator --url https://aaronstannard.com --output current.md --diff baseline.md --strict
```

**Custom retry configuration:**
```bash
link-validator --url https://example.com \
  --max-external-retries 5 \
  --retry-delay-seconds 30
```

**Using environment variables:**
```bash
export LINK_VALIDATOR_MAX_EXTERNAL_RETRIES=10
export LINK_VALIDATOR_RETRY_DELAY_SECONDS=5
link-validator --url https://example.com --strict
```

## ⚙️ Configuration

### Retry Policy Configuration

LinkValidator implements smart retry logic for external links that return `429 Too Many Requests`:

- **Max Retries:** Configure with `--max-external-retries` or `LINK_VALIDATOR_MAX_EXTERNAL_RETRIES` 
- **Retry Delay:** Configure with `--retry-delay-seconds` or `LINK_VALIDATOR_RETRY_DELAY_SECONDS`
- **Jitter:** Automatically adds ±25% jitter to prevent thundering herd problems
- **Retry-After Header:** Automatically respects `Retry-After` headers when present

### Performance Tuning

The crawler is configured for optimal performance out of the box:

- **Concurrent Requests:** 10 simultaneous requests per domain
- **Request Timeout:** 5 seconds per request
- **Actor-Based:** Leverages Akka.NET for efficient message passing and state management

## 📊 Output Format

LinkValidator generates comprehensive markdown reports showing:

### Internal Links
```markdown
## Internal Links

| URL | Status | Status Code |
|-----|--------|-------------|
| https://example.com/ | ✅ Ok | 200 |
| https://example.com/about | ✅ Ok | 200 |
| https://example.com/missing | ❌ Error | 404 |
```

### External Links
```markdown
## External Links

| URL | Status | Status Code |
|-----|--------|-------------|
| https://github.com/example | ✅ Ok | 200 |
| https://api.example.com/v1 | ❌ Error | 500 |
| https://slow-service.com | ⏸️ Retry Scheduled | 429 |
```

## 🐛 Troubleshooting

### Common Issues

**"Failed to crawl" warnings:**
- Check if the URL is accessible from your network
- Verify SSL certificates are valid
- Ensure the site doesn't block automated requests

**429 Too Many Requests errors:**
- Increase `--retry-delay-seconds` for slower retry intervals
- Reduce `--max-external-retries` to fail faster
- Some APIs have very strict rate limits

**Timeout issues:**
- Large sites may take time to crawl completely
- The tool respects `Retry-After` headers and adds jitter to delays
- External link validation happens after internal crawling completes

**Memory usage:**
- The crawler keeps discovered URLs in memory during execution
- For very large sites (>10k pages), monitor memory usage
- Consider crawling specific sections rather than entire domains

### Debug Information

Run with increased logging to diagnose issues:

```bash
# The tool outputs detailed logs during crawling
LinkValidator --url https://example.com --output debug-sitemap.md
```

### Exit Codes

- **0:** Success, all links are valid
- **1:** Error occurred (invalid URL, network issues, etc.)
- **1:** Broken links found (when using `--strict` mode)

## 🤝 Contributing

Contributions are welcome! Please see our [contributing guidelines](CONTRIBUTING.md) for details.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/Aaronontheweb/link-validator.git
cd link-validator

# Install .NET 9 SDK
# Build and test
dotnet build
dotnet test

# Run locally
dotnet run --project src/LinkValidator -- --url https://example.com
```

## 📝 License

This project is licensed under the [Apache 2.0 License](LICENSE).

## 🙏 Acknowledgments

- Built with [Akka.NET](https://getakka.net/) for high-performance actor-based concurrency
- Uses [HtmlAgilityPack](https://html-agility-pack.net/) for HTML parsing
- Powered by [System.CommandLine](https://github.com/dotnet/command-line-api) for CLI functionality
