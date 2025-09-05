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

### Prerequisites

**Required:** [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) must be installed on your system to run LinkValidator.

- **Windows:** Download the [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0/runtime)
- **Linux/macOS:** Install via package manager or download from [Microsoft](https://dotnet.microsoft.com/download/dotnet/9.0)

### Option 1: Install Script (Recommended)

**Windows (PowerShell):**
```powershell
irm https://raw.githubusercontent.com/Aaronontheweb/link-validator/dev/install.ps1 | iex
```

**Linux/macOS (Bash):**
```bash
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/dev/install.sh | bash
```

<details>
<summary>Advanced installation options</summary>

**Windows custom options:**
```powershell
# Install to custom location  
irm https://raw.githubusercontent.com/Aaronontheweb/link-validator/dev/install.ps1 | iex -ArgumentList "-InstallPath", "C:\tools\linkvalidator"

# Install without adding to PATH
irm https://raw.githubusercontent.com/Aaronontheweb/link-validator/dev/install.ps1 | iex -ArgumentList "-SkipPath"
```

**Linux/macOS custom options:**
```bash
# Install to custom location
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/dev/install.sh | bash -s -- --dir ~/.local/bin

# Install without adding to PATH
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/dev/install.sh | bash -s -- --skip-path
```
</details>

### Option 2: Download Binary

Download the appropriate binary from the [latest release](https://github.com/Aaronontheweb/link-validator/releases/latest):

- **Windows x64:** `link-validator-windows-x64.zip`
- **Linux x64:** `link-validator-linux-x64.tar.gz` 
- **macOS x64:** `link-validator-macos-x64.tar.gz`
- **macOS ARM64:** `link-validator-macos-arm64.tar.gz`

Extract and place the binary in your PATH.

**Note:** These binaries require the .NET 9 Runtime to be installed (see Prerequisites above).

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

📚 **[Complete CI/CD Integration Guide](docs/cicd-integration.md)**

The documentation includes ready-to-use examples for:
- **GitHub Actions** - Including advanced baseline comparison workflows
- **Azure DevOps** - With artifact management and parallel validation
- **Jenkins** - Both declarative and scripted pipelines
- **GitLab CI** - Multi-stage validation workflows
- **Docker** - Health checks and multi-stage builds
- **CircleCI** - Workspace and caching examples

### Quick Example

```yaml
# GitHub Actions
- name: Install LinkValidator
  run: curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/dev/install.sh | bash

- name: Validate Links
  run: link-validator --url http://localhost:3000 --strict
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

### Ignoring Links in HTML

LinkValidator supports HTML comments to exclude specific links from validation. This is useful for development URLs, local services, or intentionally broken example links.

#### Ignore Single Link

Use `<!-- link-validator-ignore -->` to ignore just the next link:

```html
<!-- link-validator-ignore -->
<a href="http://localhost:3000">This link will be ignored</a>
<a href="http://localhost:9090">This link will be validated</a>
```

#### Ignore Block of Links

Use `<!-- begin link-validator-ignore -->` and `<!-- end link-validator-ignore -->` to ignore all links within a section:

```html
<!-- begin link-validator-ignore -->
<div>
  <p>These local development links won't be validated:</p>
  <a href="http://localhost:3000">Grafana Dashboard</a>
  <a href="http://localhost:16686">Jaeger UI</a>
  <a href="http://localhost:9090">Prometheus</a>
</div>
<!-- end link-validator-ignore -->
```

**Note:** Comments are case-insensitive, so `<!-- LINK-VALIDATOR-IGNORE -->`, `<!-- Link-Validator-Ignore -->`, etc. will all work.

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
