#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY_EDITOR="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity}"
if [[ ! -x "$UNITY_EDITOR" ]]; then
  echo "Install Unity 6000.6.0f1 Apple silicon using Unity Hub, sign in and activate a license first." >&2
  exit 1
fi
METHOD=AlbionOdyssey.Editor.OdysseySetup.Prepare
[[ "${1:-}" == "build" ]] && METHOD=AlbionOdyssey.Editor.OdysseySetup.BuildMac
mkdir -p "$ROOT/Unity/Logs"
"$UNITY_EDITOR" -batchmode -quit -projectPath "$ROOT/Unity" -executeMethod "$METHOD" -logFile "$ROOT/Unity/Logs/setup.log"
