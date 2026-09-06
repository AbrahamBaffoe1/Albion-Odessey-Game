#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$ROOT/work"
"${CXX:-c++}" -std=c++17 -Wall -Wextra -Werror -pedantic -fsanitize=address,undefined -g \
  -I "$ROOT/Source/AlbionOdyssey" "$ROOT/Tests/rules_test.cpp" -o "$ROOT/work/rules_test"
"$ROOT/work/rules_test"
python3 "$ROOT/Tools/validate_project.py"
