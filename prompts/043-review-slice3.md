# Prompt 043: Review slice 3 code

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-25 · **Phase:** 4

## Prompt (verbatim)

> Review code first

## How the AI approached it

1. Re-read the matcher; listed 5 findings with an action for each.
2. Wrote tests first:
   - F1 red → UTC guard added
   - F3 pins existing behaviour
   - **F2's test refuted the AI's own claim**; rewritten to document the
     real behaviour
3. Its first mutation run silently failed (a script bug hidden by suppressed
   stderr). Noticed from the exit code, then fixed.
4. 17/17 mutants killed; hygiene scans clean.

## Outcome

- 102 tests green
- design §8 keyword-validation note
- V-053
