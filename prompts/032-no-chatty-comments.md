# Prompt 032: No chatty comments in code

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 4

## Prompt (verbatim)

> I don't want to flood the code with chatty comments like: "asp-page (not a literal action): the form tag helper only adds the antiforgery token for
>     routed forms, and Razor Pages rejects POSTs without it (bug found in review, V-038)."

## Outcome

- Every comment in `src/` and `tests/` reduced to a short why, or removed.
- No process IDs remain (grep-verified).
- Placeholder UI text no longer shows requirement IDs.
- Convention recorded as D-024 and saved to the assistant's memory for future
  slices.
- V-039.
