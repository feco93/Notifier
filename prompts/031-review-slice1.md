# Prompt 031: Review slice 1 code before committing

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 4

## Prompt (verbatim)

> Review code first

## How the AI approached it

1. Re-read every slice 1 file looking for untested behaviour, not style.
2. Treated each suspicion as a hypothesis to test:
   - logout antiforgery
   - DB path resolution
3. **Found a real bug the green test suite had missed:** logout always
   failed (no antiforgery token on a literal-action form). Wrote the failing
   test first, then fixed it.
4. Fixed the DB path. Documented the `EnsureCreated` schema caveat and the
   deliberate prototype gaps in the README.

## Outcome

- 16 tests green
- V-038
