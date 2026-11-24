# Prompt 030: Start slice 1 (skeleton, data model, login)

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 4

## Prompt (verbatim)

> Start slice 1

## How the AI approached it

1. Checked package versions and licences on NuGet before writing any
   `csproj`. Verified the last memory-based package name (V-032).
2. Used the **empty** `web` template rather than `webapp`, to avoid
   generated Bootstrap/jQuery noise.
3. Built the 4 projects, domain model, DbContext (unique index, Restrict FKs,
   UTC guard), cookie auth with config users, Login/Logout and placeholder
   pages.
4. Caught along the way:
   - its own silent-time-shift bug (V-033)
   - a false "Build succeeded" from an empty solution (V-034)
   - two wrong test expectations, one of which tested the wrong layer (V-035)
5. Mutation-checked the tests (V-036) and smoke-tested the dev accounts on
   the real app (V-037).

## Outcome

- 14 tests green
- README run instructions + dev accounts
- V-032…V-037
