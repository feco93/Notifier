# Prompt 034: Start slice 2 (BBC source assembly)

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 4

## Prompt (verbatim)

> Start slice 2

## How the AI approached it

1. Verified package versions on NuGet.
2. Added the `IArticleSource` contract to Core.
3. Implemented `Notifier.Sources.Bbc`:
   - all `internal`
   - public `AddBbcNewsSource` only (a test enforces this)
   - lenient dates
   - raw-position ranks
   - in-feed de-duplication
   - DTD prohibition and size caps
4. Wrote a **synthetic** BBC-shaped fixture covering every edge case found in
   the spikes (no real BBC content in the repo).
5. A failing test exposed a wrong assumption about parser exceptions (V-041).
6. Mutation-checked the parser (V-042).
7. Ran the real assembly against the live feeds once. Investigated an
   unexpected item-count gap down to its cause: in-feed duplicates (V-043).

## Outcome

- 46 tests green
- V-041…V-044
- live check kept in `notes/spikes/bbc-live-smoke`
