# Prompt 012: Start phase 2 (requirements)

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 2

## Prompt (verbatim)

> Start phase 2

## How the AI approached it

1. Drafted `docs/02-requirements.md`:
   - personas, glossary
   - 6 user + 8 admin stories with acceptance criteria and MoSCoW priority
   - 12 NFRs, non-goals
2. **Scripted a traceability check** (every A-xx in 01 searched for in 02, plus
   a search for leftover USGS-era terms) instead of eyeballing it.
3. The check exposed 3 assumptions that had silently gone stale through the
   USGS → BBC switch (A-06, A-09, A-12). They were fixed at the source (01),
   and the check was re-run to 0 gaps.
4. Compared the Must scope with the build budget and wrote down the cut order
   in advance.

## Outcome

`docs/02-requirements.md`; 01 revised; V-015, V-016.
