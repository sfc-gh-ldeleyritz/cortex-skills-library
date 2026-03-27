#!/usr/bin/env bash
# validate.sh -- Validate a rendered PPTX file using OpenXML validation
# Usage: .cortex/skills/pptx/scripts/validate.sh output.pptx
set -euo pipefail

if [ $# -lt 1 ]; then
    echo "Usage: validate.sh <output.pptx>" >&2
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
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

# When using Windows dotnet.exe from WSL, convert the DLL path to a Windows path
if [[ "$DOTNET" == *"dotnet.exe"* ]]; then
    DOTNET_DLL="$(wslpath -w "$DOTNET_DLL")"
fi

"$DOTNET" "${DOTNET_DLL}" validate "$@"
