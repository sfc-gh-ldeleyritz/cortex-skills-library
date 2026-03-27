#!/usr/bin/env bash
# build-and-validate.sh -- Full pipeline: validate-spec -> build -> validate
# Usage: .cortex/skills/pptx/scripts/build-and-validate.sh spec.yaml output.pptx
set -euo pipefail

if [ $# -lt 2 ]; then
    echo "Usage: build-and-validate.sh <spec.yaml> <output.pptx>" >&2
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Self-heal: ensure all sibling scripts are executable (fixes macOS first-run)
chmod +x "${SCRIPT_DIR}"/*.sh 2>/dev/null || true
DOTNET_DLL="${SCRIPT_DIR}/dotnet/SnowflakePptx/bin/Release/net9.0/SnowflakePptx.dll"
export SNOWFLAKE_TEMPLATE_PATH="${SCRIPT_DIR}/snowflake/templates/SNOWFLAKE TEMPLATE JANUARY 2026.pptx"

# Resolve dotnet executable
if command -v dotnet &>/dev/null; then
    DOTNET=dotnet
elif [ -x "/mnt/c/Program Files/dotnet/dotnet.exe" ]; then
    DOTNET="/mnt/c/Program Files/dotnet/dotnet.exe"
else
    echo "ERROR: dotnet not found. Install .NET SDK or use Windows cmd.exe." >&2
    exit 1
fi

echo "[1/3] Validating spec..."
"$DOTNET" "${DOTNET_DLL}" validate-spec --strict "$1"

echo "[2/3] Building PPTX..."
"$DOTNET" "${DOTNET_DLL}" build --spec "$1" --out "$2"

echo "[3/3] Validating output PPTX..."
"$DOTNET" "${DOTNET_DLL}" validate "$2"

echo "[OK] All steps passed."
