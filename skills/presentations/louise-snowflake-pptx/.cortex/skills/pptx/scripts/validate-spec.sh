#!/usr/bin/env bash
# validate-spec.sh -- Validate a YAML spec file before building
# Usage: .cortex/skills/pptx/scripts/validate-spec.sh spec.yaml [--strict]
set -euo pipefail

if [ $# -lt 1 ]; then
    echo "Usage: validate-spec.sh <spec.yaml> [--strict]" >&2
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

"$DOTNET" "${DOTNET_DLL}" validate-spec "$@"
