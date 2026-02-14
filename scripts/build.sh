#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MOD_DIR="$ROOT_DIR/mod"
OUT_DIR="$MOD_DIR/Assemblies"
LIB_DIR="$ROOT_DIR/lib"

mkdir -p "$OUT_DIR"

# Expand recursive glob for Source/**/*.cs
shopt -s globstar nullglob
SOURCES=("$MOD_DIR"/Source/**/*.cs)
shopt -u globstar

if [ ${#SOURCES[@]} -eq 0 ]; then
  echo "No source files found under mod/Source" >&2
  exit 1
fi

for dep in Assembly-CSharp.dll UnityEngine.dll UnityEngine.CoreModule.dll; do
  if [ ! -f "$LIB_DIR/$dep" ]; then
    echo "Missing dependency: $LIB_DIR/$dep" >&2
    exit 1
  fi
done

mcs \
  -target:library \
  -out:"$OUT_DIR/RimworldGM.dll" \
  -r:"$LIB_DIR/Assembly-CSharp.dll" \
  -r:"$LIB_DIR/UnityEngine.dll" \
  -r:"$LIB_DIR/UnityEngine.CoreModule.dll" \
  "${SOURCES[@]}"

echo "Built: $OUT_DIR/RimworldGM.dll"
