#!/usr/bin/env bash
set -euo pipefail

aspire stop \
	--apphost src/Aspire/Aspire.AppHost/Aspire.AppHost.csproj \
	--non-interactive \
	--nologo
echo "Aspire services stopped."
