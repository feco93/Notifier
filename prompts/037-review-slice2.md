# Prompt 037: Review slice 2 before committing

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 4

## Prompt (verbatim)

> Please review your changes before commiting.

## How the AI approached it

1. Re-read the slice 2 diff as code that handles untrusted input.
2. Listed 4 findings with a risk level for each.
3. Wrote the tests **first** and ran them:
   - 7 cases failed, confirming R1 and R2
   - R3's test passed, which proved the size cap already worked
4. Fixed the code:
   - http(s)-only article URLs
   - startup options validation
5. Re-ran the full suite, then the comment-convention, username-leak and
   unwanted-file scans.

## Outcome

- 54 tests green
- V-047
