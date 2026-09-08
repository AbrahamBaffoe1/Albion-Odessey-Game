"""Check the macOS bundle, signature class and optional notarization configuration."""
from __future__ import annotations
import argparse
import json
import plistlib
import os
import subprocess
from pathlib import Path


def run(*args: str) -> str:
    return subprocess.check_output(args, text=True, stderr=subprocess.STDOUT)


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser()
    parser.add_argument("--app", type=Path, default=root / "Unity/Builds/Albion Odyssey.app")
    parser.add_argument("--require-developer-id", action="store_true")
    parser.add_argument("--require-notarization", action="store_true")
    args = parser.parse_args()
    app = args.app.expanduser().resolve()
    executable = app / "Contents/MacOS/Albion Odyssey"
    if not app.is_dir() or not executable.is_file():
        raise SystemExit(f"missing macOS app: {app}")
    codesign = run("codesign", "-dv", "--verbose=4", str(app))
    run("codesign", "--verify", "--deep", "--strict", str(app))
    plist = plistlib.loads((app / "Contents/Info.plist").read_bytes())
    identity = "ad-hoc" if "Signature=adhoc" in codesign else "development"
    if "Authority=Developer ID Application:" in codesign:
        identity = "developer-id"
    notarization = "configured" if ("APPLE_NOTARY_PROFILE" in os.environ or "APPLE_ID" in os.environ) else "not-configured"
    result = {"app": str(app), "bundleId": plist.get("CFBundleIdentifier"), "version": plist.get("CFBundleShortVersionString"), "signature": identity, "notarization": notarization}
    print(json.dumps(result, indent=2))
    if args.require_developer_id and identity != "developer-id": return 3
    if args.require_notarization and notarization != "configured": return 4
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
