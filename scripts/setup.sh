#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

dotnet restore "$repository_root/Knowledge.sln"
npm ci --prefix "$repository_root/src/Knowledge.Web"
