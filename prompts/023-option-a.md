# Prompt 023: Option A: two projects + source interface

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 3

## Prompt (verbatim)

> A

## Outcome

- 03-design.md:
  - §1 rationale rewritten with a correction note
  - §2 layout: BBC code moved to `Web/Sources/Bbc`
  - new §4a `IArticleSource` contract
  - §6 attribution taken from the source
  - §10 tests: fake source proves NFR-03
- ADR 0001 layout row updated.
- D-020, V-026.
- Then applied the new process to every Must NFR. Found NFR-10 not actually
  met; fixed with design §9a (V-027).
