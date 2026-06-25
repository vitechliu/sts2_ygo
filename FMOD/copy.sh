#!/usr/bin/env sh
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SRC_BANK="$SCRIPT_DIR/Build/desktop/VYgo.bank"
SRC_GUIDS="$SCRIPT_DIR/Build/GUIDs.txt"
DEST_DIR="$SCRIPT_DIR/../VYgo/banks"
DEST_BANK="$DEST_DIR/VYgo.bank"
DEST_GUIDS="$DEST_DIR/VYgo.guids.txt"

mkdir -p "$DEST_DIR"

echo "Copying bank..."
cp "$SRC_BANK" "$DEST_BANK"

echo "Copying GUIDs..."
cp "$SRC_GUIDS" "$DEST_GUIDS"

echo "Done."
