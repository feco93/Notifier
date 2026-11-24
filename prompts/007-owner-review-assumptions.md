# Prompt 007: Owner review of assumptions + source question

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 1

## Prompt (verbatim)

> I want to change the following assumptions:
> A-01 - The application is created for personal use only. We need 2 user roles: user and admin. For v1 I'd like to use hard coded users and an admin.
> A-03 - I don't want to overcomplicate deciding the importance of news. I don't want to decide the importance automatically with simple rules. For v1 I'd like to choose only one source and I'd like to plan the algorithm based on the selected source. Which sources would be the best for this task? It must be free and easy to use.
> A-05, A-06 - I'd like to use onlye one data source which is free. It'd be good to get news about different topics using only one data source.
> A-11 - webhooks acceptable
> A-07- Let's go with polling for v1.

## How the AI approached it

1. Evaluated candidate sources with **live calls**, not from memory:
   - BBC RSS (5 feeds)
   - GDELT (3 attempts, rate-limited)
   - Guardian (the "test" key the AI remembered failed)
2. Measured BBC feed ordering, item age and cross-feed duplication to check
   whether an "importance" signal exists in the data.
3. Wrote `notes/source-evaluation.md` with a recommendation and a draft
   importance approach.
4. Updated A-01, A-05/06, A-07, A-11 and A-16. Marked A-02…A-04 as under
   revision.

## Outcome

- D-009…D-012
- V-007…V-011
- **Open:** owner to confirm the source (recommended: BBC News RSS).
