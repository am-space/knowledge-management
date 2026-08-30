#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
mode="${1:---all}"

verify_backend() {
  dotnet build "$repository_root/Knowledge.sln" --no-restore
  dotnet test "$repository_root/tests/Knowledge.Server.UnitTests/Knowledge.Server.UnitTests.csproj" \
    --no-build --logger "trx;LogFileName=unit-tests.trx"
}

verify_frontend() {
  npm run lint --prefix "$repository_root/src/Knowledge.Web"
  npm run typecheck --prefix "$repository_root/src/Knowledge.Web"
  npm test --prefix "$repository_root/src/Knowledge.Web"
  npm run build --prefix "$repository_root/src/Knowledge.Web"
}

verify_integration() {
  if [[ -z "${KNOWLEDGE_TEST_POSTGRES:-}" ]]; then
    if ! command -v docker >/dev/null 2>&1 || ! docker compose version >/dev/null 2>&1; then
      echo "PostgreSQL integration tests require Docker Compose or KNOWLEDGE_TEST_POSTGRES." >&2
      return 1
    fi

    docker compose --file "$repository_root/compose.yaml" up --detach --wait postgres
    export KNOWLEDGE_TEST_POSTGRES="Host=127.0.0.1;Port=${POSTGRES_PORT:-54329};Database=${POSTGRES_DB:-knowledge_test};Username=${POSTGRES_USER:-knowledge};Password=${POSTGRES_PASSWORD:-knowledge-dev-only}"
  fi

  dotnet test "$repository_root/tests/Knowledge.Server.IntegrationTests/Knowledge.Server.IntegrationTests.csproj" \
    --no-build --logger "trx;LogFileName=integration-tests.trx"
}

case "$mode" in
  --all)
    verify_backend
    verify_frontend
    verify_integration
    ;;
  --backend)
    verify_backend
    ;;
  --frontend)
    verify_frontend
    ;;
  --integration)
    dotnet build "$repository_root/Knowledge.sln" --no-restore
    verify_integration
    ;;
  *)
    echo "Usage: scripts/verify.sh [--all|--backend|--frontend|--integration]" >&2
    exit 2
    ;;
esac
