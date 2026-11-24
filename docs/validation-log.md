# Validation Log

Every non-trivial AI output gets an entry: what it produced, how I checked it,
and the outcome. This file is the main evidence of critical review.

**Outcome:** ✅ accepted · ✏️ accepted with changes · ❌ rejected

| ID | Date | Phase | AI output | Check applied | Outcome | Notes / correction |
|---|---|---|---|---|---|---|
| V-001 | 2026-11-24 | 0 | High-level plan (prompt 001) | Read against the assignment's evaluation criteria | ✏️ | Kept structure. Stack left as a proposal (D-003) rather than accepted silently. |
| V-002 | 2026-11-24 | 0 | AI pushed for a stack decision before any requirements existed | Questioned ordering: does anything in phases 1–3 need a stack? No. | ❌ | Premature anchoring. Stack decision moved to end of phase 3 (D-004). |
| V-003 | 2026-11-24 | 0 | Re-scoped plan v2 | Phase budgets sum checked (7.25h + 0.75h buffer = 8h); slice estimates sum to the 3.5h build phase; v1's "in git history" claim was false (nothing committed yet) | ✏️ | Fixed the false git-history claim in plan.md. |

## Standard checklist applied to generated code

- [ ] External APIs / endpoints / fields verified by a real call
- [ ] Library functions/params exist in the pinned version
- [ ] Tests fail when the logic is broken (mutation spot-check)
- [ ] No swallowed exceptions / silent fallbacks
- [ ] Timezones explicit (UTC stored)
- [ ] Idempotency / dedup holds under re-runs
- [ ] No secrets in logs, code or repo
- [ ] No scope creep beyond the requested slice
