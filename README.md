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
- **Cross-Platform** - Native binaries for Windows x64, Linux x64/ARM64, and Apple Silicon macOS
- **Diff Support** - Compare current crawl results against previous runs to detect changes
- **Authenticated Crawling** - Supply a cookie file to validate links behind login pages
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

Released binaries are self-contained Native AOT executables; no .NET runtime is required.

### Option 1: Install Script (Recommended)

**Windows (PowerShell):**
```powershell
irm https://raw.githubusercontent.com/Aaronontheweb/link-validator/dev/install.ps1 | iex
```

**Linux or Apple Silicon macOS (Bash):**
```bash
curl -fsSL https://raw.githubusercontent.com/Aaronontheweb/link-validator/dev/install.sh | bash
```

<details>
<summary>Advanced installation options</summary>

**Windows custom options:**
```powershell
# Install to custom location  
& ([scriptblock]::Create((irm https://raw.githubusercontent.com/Aaronontheweb/link-validator/dev/install.ps1))) -InstallPath "C:\tools\linkvalidator"

# Install without adding to PATH
& ([scriptblock]::Create((irm https://raw.githubusercontent.com/Aaronontheweb/link-validator/dev/install.ps1))) -SkipPath
```

**Linux or Apple Silicon macOS custom options:**
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
- **Linux ARM64:** `link-validator-linux-arm64.tar.gz`
- **macOS ARM64 (Apple Silicon):** `link-validator-macos-arm64.tar.gz`

Extract and place the binary in your PATH.

### Option 3: Build from Source

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
git clone https://github.com/Aaronontheweb/link-validator.git
cd link-validator

# Build and run locally
dotnet run --project src/LinkValidator -- --url https://example.com

# Or publish as a self-contained Native AOT binary
dotnet publish src/LinkValidator -c Release -r <RUNTIME> -p:PublishAot=true
# Where <RUNTIME> is: win-x64, linux-x64, linux-arm64, or osx-arm64
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
| `--strict` | Return error code for internal 400+ responses or external 404/410 responses | `false` |
| `--cookie-file <PATH>` | Netscape/Mozilla cookie file for authenticated crawling | - |
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

### Validating Authenticated Pages

Many web applications require authentication to access most of their pages. Without credentials, the crawler only sees the login page. The `--cookie-file` option lets you pass session cookies so the crawler can reach authenticated areas.

The cookie file uses the [Netscape/Mozilla cookie format](https://curl.se/docs/http-cookies.html) — the same format produced by `curl -c`.

#### Step 1: Acquire a session cookie

Use `curl -c` to authenticate and save the resulting cookies:

```bash
# For apps with a dev/test login endpoint
curl -c cookies.txt -L http://localhost:5000/dev-login

# For apps with a form-based login
curl -c cookies.txt -L -d "username=admin&password=secret" http://localhost:5000/login

# For APIs that return a Set-Cookie header
curl -c cookies.txt -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"secret"}'
```

#### Step 2: Crawl with the cookie file

```bash
link-validator --url http://localhost:5000 --cookie-file cookies.txt --output report.md
```

The cookies are shared across all crawler workers, so every page request includes the session cookie. The crawler will discover and validate links on authenticated pages just like unauthenticated ones.

#### CI/CD example (GitHub Actions)

```yaml
- name: "Get auth cookie"
  run: curl -c cookies.txt -L -s http://localhost:5000/dev-login

- name: "Validate links"
  run: |
    link-validator \
      --url http://localhost:5000 \
      --cookie-file cookies.txt \
      --output link-report.md \
      --max-external-retries 3
```

#### Tips

- **Use a test/dev login endpoint** — avoid putting real credentials in CI pipelines.
- **Cookie expiry** — session cookies from `curl -c` typically last long enough for a crawl. If your sessions are very short-lived, increase the session timeout in your test configuration.
- **HttpOnly cookies** — `curl -c` writes HttpOnly cookies with a `#HttpOnly_` prefix. LinkValidator handles this automatically.

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

**Strict mode for CI (fails on internal 400+ responses and external 404/410 responses):**
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

- **0:** Crawl completed without a strict-mode violation
- **1:** Error occurred (invalid URL, network issues, etc.)
- **1:** Internal 400+ or external 404/410 responses found (when using `--strict` mode)

## 🤝 Contributing

Contributions are welcome! Please see our [contributing guidelines](CONTRIBUTING.md) for details.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/Aaronontheweb/link-validator.git
cd link-validator

# Install .NET 10 SDK
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
