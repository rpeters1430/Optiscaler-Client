#!/bin/bash
# OptiScaler Client Linux Build Helper
set -e

echo "=== OptiScaler Client Linux Build Helper ==="

# Check for dotnet SDK
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is not installed."
    echo "Please install the .NET SDK (version 10.0 or later) from your package manager."
    echo "Example (Arch/CachyOS): sudo pacman -S dotnet-sdk"
    exit 1
fi

echo "Restoring packages..."
dotnet restore

echo "Compiling self-contained release build for Linux (x64)..."
dotnet publish -c Release -r linux-x64 --self-contained

echo ""
echo "=== Build Succeeded! ==="
echo "The self-contained executable is located at:"
echo "bin/Release/net10.0/linux-x64/publish/OptiscalerClient"
echo ""
echo "To run it, use:"
echo "chmod +x bin/Release/net10.0/linux-x64/publish/OptiscalerClient"
echo "./bin/Release/net10.0/linux-x64/publish/OptiscalerClient"
