#!/usr/bin/env bash
set -euo pipefail

# Creative PPTX build wrapper for macOS/Linux
# Usage: ./creative-build.sh build --spec spec.yaml --out output.pptx
#        ./creative-build.sh validate spec.yaml

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/node/SnowflakeCreativePptx"

# Auto-install node_modules if missing
if [ ! -d "$PROJECT_DIR/node_modules" ]; then
    echo "[creative-build] Installing dependencies..."
    (cd "$PROJECT_DIR" && npm install)
fi

# Forward all arguments to the Node.js entry point
exec node "$PROJECT_DIR/index.js" "$@"
