# Knowledge.E2E.Tests

Playwright/Chromium verification of the local Article workflow through the real React client,
HTTP server, and SQLite database. Dependencies are independent of the shipped frontend bundle.

```bash
scripts/setup.sh
scripts/verify.sh --e2e
```

Use the canonical script: it provisions and removes a temporary SQLite database. Playwright starts
and stops dedicated loopback servers on ports 5081 and 5174, refuses existing servers, and uses a
fresh browser context. The workflow covers create, exact Markdown preview, reopen after a page
reload, edit, revision saves, a real conflicting writer, draft preservation, and empty Markdown.
It does not mock API responses or use the developer's database.

On Linux, missing browser system libraries can be installed with:

```bash
npm run install-browser --prefix tests/Knowledge.E2E.Tests -- --with-deps
```

Failure traces and screenshots remain under ignored `test-results/`. Full verification and CI
include this suite. See [testing](../../docs/testing.md) for coverage and limitations.
