#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

dotnet restore "$repository_root/Knowledge.sln"
dotnet tool restore --tool-manifest "$repository_root/.config/dotnet-tools.json"
npm ci --prefix "$repository_root/src/Knowledge.Web"
