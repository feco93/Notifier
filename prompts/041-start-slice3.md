# Prompt 041: Start slice 3 (matcher)

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-25 · **Phase:** 4

## Prompt (verbatim)

> Start slice 3

## How the AI approached it

1. Re-read design §4.
2. Wrote 30 tests first; confirmed red (no compile).
3. Implemented the pure matcher in Core, with a `Rejection` result so tests
   assert *which* rule failed.
4. Ran 11 mutations, one per rule. All killed.
5. Self-reviewed as a user. Found that the **spec's** substring keyword
   matching is noisy, measured it on real captures, and put it to the owner
   instead of changing it silently.

## Outcome

- 86 tests green
- design §4 updated (signature, source-neutral reasons)
- V-050, V-051
- keyword decision pending
