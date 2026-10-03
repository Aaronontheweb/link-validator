#!/usr/bin/env bash
# Hermetic smoke test for install.sh. A local fake GitHub Releases API and
# stand-in archives exercise platform detection and download/extract/install.

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
INSTALL_SH="$ROOT_DIR/install.sh"
VERSION="v0.0.0-smoke"
WORK="$(mktemp -d)"
SERVE="$WORK/serve"
SHIM="$WORK/shim"
SERVER_PID=""
PASS=0
FAIL=0

pass() { echo "PASS: $1"; PASS=$((PASS + 1)); }
fail() { echo "FAIL: $1"; FAIL=$((FAIL + 1)); }

cleanup() {
    if [[ -n "$SERVER_PID" ]]; then
        kill "$SERVER_PID" 2>/dev/null || true
    fi
    rm -rf "$WORK"
}
trap cleanup EXIT

mkdir -p "$SERVE/releases/tags" "$SERVE/downloads/$VERSION" "$SHIM" "$WORK/bin"

for rid in linux-x64 linux-arm64 macos-arm64; do
    binary_dir="$WORK/bin/$rid"
    mkdir -p "$binary_dir"
    printf '#!/usr/bin/env sh\necho "link-validator %s smoke"\n' "$rid" > "$binary_dir/link-validator"
    chmod +x "$binary_dir/link-validator"
    tar -czf "$SERVE/downloads/$VERSION/link-validator-$rid.tar.gz" \
        -C "$binary_dir" link-validator
done

if [[ -n "${LINK_VALIDATOR_SMOKE_ARCHIVE:-}" ]]; then
    case "${LINK_VALIDATOR_SMOKE_PLATFORM:-}" in
        linux-x64|linux-arm64|macos-arm64) ;;
        *)
            echo "LINK_VALIDATOR_SMOKE_PLATFORM must name a supported Unix platform" >&2
            exit 1
            ;;
    esac
    if [[ ! -f "$LINK_VALIDATOR_SMOKE_ARCHIVE" ]]; then
        echo "Candidate archive not found: $LINK_VALIDATOR_SMOKE_ARCHIVE" >&2
        exit 1
    fi
    expected_archive_name="link-validator-$LINK_VALIDATOR_SMOKE_PLATFORM.tar.gz"
    if [[ "$(basename "$LINK_VALIDATOR_SMOKE_ARCHIVE")" != "$expected_archive_name" ]]; then
        echo "Candidate archive must be named $expected_archive_name" >&2
        exit 1
    fi
    cp "$LINK_VALIDATOR_SMOKE_ARCHIVE" "$SERVE/downloads/$VERSION/$expected_archive_name"
fi

printf '%s\n' \
    '#!/usr/bin/env sh' \
    'case "${1:-}" in' \
    '    -s) printf '\''%s\n'\'' "${SMOKE_UNAME_S:?}" ;;' \
    '    -m) printf '\''%s\n'\'' "${SMOKE_UNAME_M:?}" ;;' \
    '    *)  printf '\''%s\n'\'' "${SMOKE_UNAME_S:?}" ;;' \
    'esac' > "$SHIM/uname"
chmod +x "$SHIM/uname"

PORT="$(python3 -c 'import socket; s=socket.socket(); s.bind(("127.0.0.1", 0)); print(s.getsockname()[1]); s.close()')"
BASE_URL="http://127.0.0.1:$PORT"

assets=""
for rid in linux-x64 linux-arm64 macos-arm64; do
    [[ -n "$assets" ]] && assets+=","
    assets+="{\"name\": \"link-validator-$rid.tar.gz\", \"browser_download_url\": \"$BASE_URL/downloads/$VERSION/link-validator-$rid.tar.gz\"}"
done
release_json="{\"tag_name\": \"$VERSION\", \"assets\": [$assets]}"
printf '%s\n' "$release_json" > "$SERVE/releases/latest"
printf '%s\n' "$release_json" > "$SERVE/releases/tags/$VERSION"

python3 "$ROOT_DIR/scripts/smoke/release-server.py" "$SERVE" "$PORT" \
    > "$WORK/http.out" 2> "$WORK/http.err" &
SERVER_PID=$!

ready=false
for _ in $(seq 1 50); do
    if curl -fsS "$BASE_URL/releases/latest" >/dev/null 2>&1; then
        ready=true
        break
    fi
    sleep 0.1
done
if [[ "$ready" != true ]]; then
    echo "Local release server failed to start" >&2
    exit 1
fi

check_install() {
    local description="$1" os="$2" machine="$3" expected_rid="$4"
    local install_dir="$WORK/install-$expected_rid"
    local output rc

    set +e
    if [[ $# -eq 5 ]]; then
        output=$(HOME="$WORK/home" PATH="$SHIM:$PATH" \
            SMOKE_UNAME_S="$os" SMOKE_UNAME_M="$machine" \
            LINK_VALIDATOR_GITHUB_API_URL="$BASE_URL" \
            bash "$INSTALL_SH" --dir "$install_dir" --skip-path --version "$5" 2>&1)
    else
        output=$(HOME="$WORK/home" PATH="$SHIM:$PATH" \
            SMOKE_UNAME_S="$os" SMOKE_UNAME_M="$machine" \
            LINK_VALIDATOR_GITHUB_API_URL="$BASE_URL" \
            bash "$INSTALL_SH" --dir "$install_dir" --skip-path 2>&1)
    fi
    rc=$?
    set -e

    local binary_output=""
    local binary_rc=1
    if [[ -x "$install_dir/link-validator" ]]; then
        set +e
        binary_output=$("$install_dir/link-validator" --version 2>&1)
        binary_rc=$?
        set -e
    fi

    if [[ $rc -eq 0 && $binary_rc -eq 0 ]] \
        && { [[ "${LINK_VALIDATOR_SMOKE_PLATFORM:-}" == "$expected_rid" ]] \
            || grep -q "$expected_rid smoke" <<< "$binary_output"; }; then
        pass "$description installs $expected_rid"
    else
        fail "$description (exit=$rc)"
        printf '%s\n' "$output"
        printf '%s\n' "$binary_output"
    fi
}

check_install "Linux x64 latest release" Linux x86_64 linux-x64
check_install "Linux ARM64 pinned release" Linux aarch64 linux-arm64 "$VERSION"
check_install "Apple Silicon macOS pinned release" Darwin arm64 macos-arm64 "$VERSION"

set +e
intel_output=$(HOME="$WORK/home" PATH="$SHIM:$PATH" \
    SMOKE_UNAME_S=Darwin SMOKE_UNAME_M=x86_64 \
    LINK_VALIDATOR_GITHUB_API_URL="$BASE_URL" \
    bash "$INSTALL_SH" --dir "$WORK/intel-macos" --skip-path 2>&1)
intel_rc=$?
set -e
if [[ $intel_rc -ne 0 ]] && grep -q "Apple Silicon is required" <<< "$intel_output"; then
    pass "Intel macOS is rejected clearly"
else
    fail "Intel macOS should be rejected (exit=$intel_rc)"
    printf '%s\n' "$intel_output"
fi

echo "$PASS passed, $FAIL failed"
[[ $FAIL -eq 0 ]]
