# Prompt 054: Resolve the EF multiple-collection-include warning

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-25 · **Phase:** 4

## Prompt (verbatim)

> Could you resolve the following warning?
> warn: Microsoft.EntityFrameworkCore.Query[20504]
>       Compiling a query which loads related collections for more than one collection navigation, either via 'Include' or through projection, but no 'QuerySplittingBehavior' has been configured. By default, Entity Framework will use 'QuerySplittingBehavior.SingleQuery', which can potentially result in slow query performance. See https://go.microsoft.com/fwlink/?linkid=2134277 for more information. To identify the query that's triggering this warning call 'ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))'

## How the AI approached it

1. Made the warning throw for every context (app + tests). 40 tests went red,
   which identified the queries.
2. Chose `AsSingleQuery()` explicitly, with reasoning (tiny join, consistent
   snapshot).
3. Found a third occurrence in test code that its `src/`-only search had
   missed.
4. Verified:
   - mutation on both fixes
   - the real app with both queries exercised: 0 warnings
5. Left the owner's running app untouched; built into a separate output
   folder instead.

## Outcome

- 234 tests + 1 gated E2E
- V-069
