# Tests

.NET test projects live outside `src/` so production projects and test-only dependencies remain
separate. Test folders should mirror the relevant server modules without reproducing unnecessary
project boundaries.

Frontend component and hook tests should normally be colocated with their source files. Browser
end-to-end tests belong in `Knowledge.E2E.Tests`.

