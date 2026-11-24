# Prompt 013: Owner re-prioritizes requirements

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 2

## Prompt (verbatim)

> I want to move the "AS-04", "AS-05" requirements into the "Should" category.
> I also want to move the "US-04" requirement into the "Must" category.
> I also want to move the "AS-03" requirement into the "Could" category.

## How the AI approached it

1. Applied the four priority changes in `02-requirements.md` and updated the
   scope/budget table.
2. Clarified that lowering AS-05 (log **view**) does not lower NFR-04
   (log **recording**). Explained how Must deliveries are verified without the
   view.
3. Checked `plan.md` for knock-on effects. It found three contradictions,
   plus build slices still describing the abandoned USGS design (V-017).
   Fixed all of them, then grep-checked for leftover USGS-era terms.

## Outcome

D-014, V-017; `02-requirements.md` and `plan.md` updated.
