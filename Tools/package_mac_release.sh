#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP="${1:-$ROOT/Unity/Builds/Albion Odyssey.app}"
OUT="${2:-$ROOT/Unity/Releases}"
mkdir -p "$OUT"

if [[ ! -d "$APP" ]]; then
  echo "Build the macOS app first: $APP" >&2
  exit 2
fi

# Use an Apple Developer identity when supplied. Without credentials, create an
# ad-hoc signature so local testers receive a sealed, reproducible app bundle.
IDENTITY="${APPLE_SIGNING_IDENTITY:--}"
codesign --deep --force --timestamp=none --sign "$IDENTITY" "$APP"
codesign --verify --deep --strict "$APP"

NAME="$(basename "$APP" .app)"
ZIP="$OUT/$NAME-macOS.zip"
ditto -c -k --sequesterRsrc --keepParent "$APP" "$ZIP"
shasum -a 256 "$ZIP" > "$ZIP.sha256"
cat > "$OUT/release-manifest.json" <<EOF
{
  "product": "Albion Odyssey",
  "version": "0.11.0",
  "platform": "macOS",
  "app": "$(basename "$APP")",
  "archive": "$(basename "$ZIP")",
  "signature": "${APPLE_SIGNING_IDENTITY:-ad-hoc}",
  "sha256": "$(cut -d ' ' -f 1 "$ZIP.sha256")"
}
EOF
echo "PACKAGED_RELEASE_OK $ZIP"
