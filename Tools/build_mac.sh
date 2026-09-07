#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
if [[ -z "${UE_ROOT:-}" ]]; then
  echo 'Set UE_ROOT to your installed Unreal Engine folder (the folder containing Engine).'
  exit 2
fi
BUILD="$UE_ROOT/Engine/Build/BatchFiles/Mac/Build.sh"
EDITOR="$UE_ROOT/Engine/Binaries/Mac/UnrealEditor.app/Contents/MacOS/UnrealEditor"
[[ -f "$BUILD" && -x "$EDITOR" ]] || { echo 'UE_ROOT does not contain a usable Unreal Engine installation.'; exit 2; }
bash "$BUILD" AlbionOdysseyEditor Mac Development "$ROOT/AlbionOdyssey.uproject" -waitmutex
"$EDITOR" "$ROOT/AlbionOdyssey.uproject" -ExecutePythonScript="$ROOT/Tools/setup_all.py"
