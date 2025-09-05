#### 0.1.1 September 5th 2025 ####

**New Features:**
- **HTML Comment-Based Link Ignoring** - Added ability to exclude specific links from validation using HTML comments ([#70](https://github.com/Aaronontheweb/link-validator/pull/70))
  - Use `<!-- link-validator-ignore -->` to ignore the next link
  - Use `<!-- begin link-validator-ignore -->` / `<!-- end link-validator-ignore -->` to ignore blocks of links
  - Comments are case-insensitive and W3C compliant
  - Perfect for development URLs, local services, or intentionally broken example links

**Bug Fixes:**
- **Fixed External URL Processing** - External URLs now preserve their original scheme and port instead of being incorrectly modified to match the base URL ([#70](https://github.com/Aaronontheweb/link-validator/pull/70))
- **Install Script Improvements** - Fixed multiple issues with installation scripts ([#69](https://github.com/Aaronontheweb/link-validator/pull/69), [#68](https://github.com/Aaronontheweb/link-validator/pull/68))
  - Fixed bash script line endings (CRLF to LF conversion)
  - Fixed JSON parsing for single-line GitHub API responses
  - Fixed PowerShell PATH variable escaping syntax
  - Added .NET runtime requirement documentation and detection

**Documentation:**
- Added comprehensive documentation for HTML comment-based link ignoring feature
- Added Prerequisites section explaining .NET 9 Runtime requirement
- Simplified installation instructions with one-liner commands

#### 0.1.0 September 4th 2025 ####

**Initial Release**

LinkValidator is a fast, reliable CLI tool for crawling websites and validating both internal and external links. Built with Akka.NET for high-performance concurrent crawling.

**Features:**
- **Fast Concurrent Crawling** - Leverages Akka.NET actors for efficient parallel processing of websites
- **Smart External Link Handling** - Implements intelligent rate limiting with configurable retry policies for HTTP 429 responses
- **Comprehensive Reporting** - Generates detailed markdown reports of all discovered links and their status
- **Broken Link Tracking** - Tracks broken external links and provides detailed reporting on link status
- **Advanced Error Handling** - Properly handles HTTP status codes 400+ as errors while allowing redirects and other success responses
- **Link Graph Analysis** - Tracks relationships between pages to identify where broken links originate ([#32](https://github.com/Aaronontheweb/link-validator/pull/32))
- **CI/CD Ready** - Perfect for automated testing in build pipelines with strict mode and exit codes
- **Cross-Platform** - Single-file binaries for Windows, Linux, and macOS (Intel + Apple Silicon)
- **Flexible Configuration** - CLI flags and environment variables for easy customization

**Major Components:**
- **HTTP 429 Rate Limit Handling** - Automatically detects and respects `Retry-After` headers, with configurable fallback delays and jitter to prevent thundering herd issues ([#63](https://github.com/Aaronontheweb/link-validator/pull/63))
- **External Link Validation** - Comprehensive tracking and reporting of broken external URLs separate from internal site structure ([#62](https://github.com/Aaronontheweb/link-validator/pull/62))
- **Smart HTTP Status Code Handling** - Only treats HTTP status codes 400 and above as errors, properly handling redirects and other success responses ([#61](https://github.com/Aaronontheweb/link-validator/pull/61))
- **Release Infrastructure** - Complete release automation with configuration options and build system integration ([#64](https://github.com/Aaronontheweb/link-validator/pull/64))

**Installation:**
- Install scripts available for Windows PowerShell and Linux/macOS Bash
- Pre-built single-file binaries for all major platforms  
- Build from source with .NET 9 SDK

**Command Line Options:**
- `--url <URL>` - The website URL to crawl (required)
- `--output <PATH>` - Save sitemap report to file
- `--diff <PATH>` - Compare against previous sitemap file
- `--strict` - Return error code if broken links found
- `--max-external-retries <N>` - Max retries for external 429 responses (default: 3)
- `--retry-delay-seconds <N>` - Default retry delay when no Retry-After header present (default: 10)

**Environment Variable Support:**
- `LINK_VALIDATOR_MAX_EXTERNAL_RETRIES` - Configure max retry attempts
- `LINK_VALIDATOR_RETRY_DELAY_SECONDS` - Configure retry delay timing
