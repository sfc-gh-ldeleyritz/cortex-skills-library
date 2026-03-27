#!/usr/bin/env bash
# build.sh -- Snowflake PPTX build wrapper for macOS/Linux
# Usage: .cortex/skills/pptx/scripts/build.sh <spec.yaml> <output.pptx>
set -euo pipefail

if [ $# -lt 2 ]; then
    echo "Usage: build.sh <spec.yaml> <output.pptx>" >&2
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export SNOWFLAKE_TEMPLATE_PATH="${SCRIPT_DIR}/snowflake/templates/SNOWFLAKE TEMPLATE JANUARY 2026.pptx"
DOTNET_DLL="${SCRIPT_DIR}/dotnet/SnowflakePptx/bin/Release/net9.0/SnowflakePptx.dll"

# Resolve dotnet executable
if command -v dotnet &>/dev/null; then
    DOTNET=dotnet
elif [ -x "/mnt/c/Program Files/dotnet/dotnet.exe" ]; then
    DOTNET="/mnt/c/Program Files/dotnet/dotnet.exe"
else
    echo "ERROR: dotnet not found. Install .NET SDK or use Windows cmd.exe." >&2
    exit 1
fi

# When using Windows dotnet.exe from WSL, convert paths to Windows format
if [[ "$DOTNET" == *"dotnet.exe"* ]]; then
    DOTNET_DLL="$(wslpath -w "$DOTNET_DLL")"
    export SNOWFLAKE_TEMPLATE_PATH="$(wslpath -w "$SNOWFLAKE_TEMPLATE_PATH")"
fi

"$DOTNET" "${DOTNET_DLL}" build --spec "$1" --out "$2"
