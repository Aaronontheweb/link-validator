#!/usr/bin/env bash

# LinkValidator Installation Script
# Downloads and installs the latest release of LinkValidator

set -euo pipefail

# Configuration
REPO_OWNER="stannardlabs"
REPO_NAME="link-validator"
GITHUB_API_URL="https://api.github.com/repos/${REPO_OWNER}/${REPO_NAME}"
DEFAULT_INSTALL_DIR="${HOME}/.linkvalidator"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Functions
log_info() {
    echo -e "${CYAN}$1${NC}"
}

log_success() {
    echo -e "${GREEN}$1${NC}"
}

log_warning() {
    echo -e "${YELLOW}$1${NC}"
}

log_error() {
    echo -e "${RED}$1${NC}" >&2
}

show_usage() {
    cat << EOF
LinkValidator Installation Script

USAGE:
    $0 [OPTIONS]

OPTIONS:
    -d, --dir <PATH>        Installation directory (default: ${DEFAULT_INSTALL_DIR})
    -v, --version <VERSION> Specific version to install (default: latest)
    -p, --add-to-path       Add installation directory to PATH
    -h, --help             Show this help message

EXAMPLES:
    $0                                  # Install latest to default location
    $0 -d /usr/local/bin               # Install to custom directory
    $0 -v v1.0.0 --add-to-path        # Install specific version and add to PATH
    $0 --dir ./tools --version v1.0.0  # Install specific version to local directory

EOF
}

detect_platform() {
    local os
    local arch
    
    # Detect OS
    case "$(uname -s)" in
        Linux*)     os="linux" ;;
        Darwin*)    os="macos" ;;
        *)          
            log_error "Unsupported operating system: $(uname -s)"
            exit 1
            ;;
    esac
    
    # Detect architecture
    case "$(uname -m)" in
        x86_64|amd64)   arch="x64" ;;
        arm64|aarch64)  
            if [[ "$os" == "macos" ]]; then
                arch="arm64"
            else
                arch="x64"  # Fall back to x64 for Linux ARM
            fi
            ;;
        *)
            log_warning "Unsupported architecture $(uname -m), falling back to x64"
            arch="x64"
            ;;
    esac
    
    echo "${os}-${arch}"
}

fetch_release_info() {
    local version="$1"
    local url
    
    if [[ "$version" == "latest" ]]; then
        url="${GITHUB_API_URL}/releases/latest"
        log_info "Fetching latest release..."
    else
        url="${GITHUB_API_URL}/releases/tags/${version}"
        log_info "Fetching release: $version"
    fi
    
    if ! curl -fsSL -H "User-Agent: LinkValidator-Installer" "$url"; then
        log_error "Failed to fetch release information from: $url"
        exit 1
    fi
}

download_and_install() {
    local install_dir="$1"
    local platform="$2"
    local release_json="$3"
    
    # Parse release info
    local version
    version=$(echo "$release_json" | grep '"tag_name"' | sed 's/.*"tag_name": *"\([^"]*\)".*/\1/')
    
    if [[ -z "$version" ]]; then
        log_error "Could not parse version from release info"
        exit 1
    fi
    
    log_success "Version: $version"
    
    # Find appropriate asset
    local asset_name="link-validator-${platform}.tar.gz"
    local download_url
    download_url=$(echo "$release_json" | grep -A 3 "\"name\": \"$asset_name\"" | grep '"browser_download_url"' | sed 's/.*"browser_download_url": *"\([^"]*\)".*/\1/')
    
    if [[ -z "$download_url" ]]; then
        log_error "Could not find asset for platform: $platform"
        log_info "Available assets:"
        echo "$release_json" | grep '"name"' | sed 's/.*"name": *"\([^"]*\)".*/  - \1/'
        exit 1
    fi
    
    # Create temporary directory
    local temp_dir
    temp_dir=$(mktemp -d)
    trap "rm -rf '$temp_dir'" EXIT
    
    # Download
    log_info "Downloading $asset_name..."
    local archive_path="${temp_dir}/${asset_name}"
    if ! curl -fsSL -H "User-Agent: LinkValidator-Installer" -o "$archive_path" "$download_url"; then
        log_error "Failed to download $asset_name"
        exit 1
    fi
    
    # Extract
    log_info "Extracting..."
    if ! tar -xzf "$archive_path" -C "$temp_dir"; then
        log_error "Failed to extract archive"
        exit 1
    fi
    
    # Create install directory
    log_info "Creating install directory: $install_dir"
    mkdir -p "$install_dir"
    
    # Move binary
    local binary_name="LinkValidator"
    local source_path="${temp_dir}/${binary_name}"
    local dest_path="${install_dir}/${binary_name}"
    
    if [[ ! -f "$source_path" ]]; then
        log_error "Binary not found in archive: $source_path"
        exit 1
    fi
    
    mv "$source_path" "$dest_path"
    chmod +x "$dest_path"
    
    log_success "✓ LinkValidator installed successfully!"
    log_info "Binary location: $dest_path"
    
    # Test installation
    log_info "Testing installation..."
    if "$dest_path" --version; then
        log_success "✓ Installation test passed!"
    else
        log_warning "Installation test failed, but binary was installed"
    fi
}

add_to_path() {
    local install_dir="$1"
    
    log_info "Adding to PATH..."
    
    # Determine shell profile
    local shell_profile
    if [[ -n "${BASH_VERSION:-}" ]]; then
        shell_profile="${HOME}/.bashrc"
    elif [[ -n "${ZSH_VERSION:-}" ]]; then
        shell_profile="${HOME}/.zshrc"
    elif [[ -f "${HOME}/.zshrc" ]]; then
        shell_profile="${HOME}/.zshrc"
    elif [[ -f "${HOME}/.bashrc" ]]; then
        shell_profile="${HOME}/.bashrc"
    else
        shell_profile="${HOME}/.profile"
    fi
    
    # Check if already in PATH
    if grep -q "$install_dir" "$shell_profile" 2>/dev/null; then
        log_success "✓ Already in PATH ($shell_profile)"
        return
    fi
    
    # Add to PATH
    local path_line="export PATH=\"${install_dir}:\$PATH\""
    echo "" >> "$shell_profile"
    echo "# Added by LinkValidator installer" >> "$shell_profile"
    echo "$path_line" >> "$shell_profile"
    
    log_success "✓ Added to PATH in $shell_profile"
    log_info "Run 'source $shell_profile' or restart your terminal to use 'LinkValidator' command"
}

# Main script
main() {
    local install_dir="$DEFAULT_INSTALL_DIR"
    local version="latest"
    local add_to_path_flag=false
    
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            -d|--dir)
                install_dir="$2"
                shift 2
                ;;
            -v|--version)
                version="$2"
                shift 2
                ;;
            -p|--add-to-path)
                add_to_path_flag=true
                shift
                ;;
            -h|--help)
                show_usage
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                show_usage
                exit 1
                ;;
        esac
    done
    
    # Expand tilde in install directory
    install_dir="${install_dir/#~/$HOME}"
    
    log_success "LinkValidator Installer"
    
    # Detect platform
    local platform
    platform=$(detect_platform)
    log_info "Platform: $platform"
    log_info "Install directory: $install_dir"
    
    # Check dependencies
    for cmd in curl tar grep sed; do
        if ! command -v "$cmd" >/dev/null 2>&1; then
            log_error "Required command not found: $cmd"
            exit 1
        fi
    done
    
    # Fetch release information
    local release_json
    release_json=$(fetch_release_info "$version")
    
    # Download and install
    download_and_install "$install_dir" "$platform" "$release_json"
    
    # Add to PATH if requested
    if [[ "$add_to_path_flag" == true ]]; then
        add_to_path "$install_dir"
    else
        log_warning "Run with --add-to-path to add to your PATH, or use the full path:"
        log_warning "  ${install_dir}/LinkValidator --url https://example.com"
    fi
    
    log_success "Installation complete!"
}

# Run main function
main "$@"