# Scripts

`setup.sh` restores .NET packages and installs the locked frontend dependencies. `verify.sh` is the
canonical local and CI verification entry point.

```bash
scripts/setup.sh
scripts/verify.sh --all
```

Use `--backend`, `--frontend`, or `--integration` for a focused lane. PostgreSQL integration tests
use `KNOWLEDGE_TEST_POSTGRES` when supplied; otherwise they require Docker Compose and start the
repository's PostgreSQL service.
