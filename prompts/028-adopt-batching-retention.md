# Prompt 028: Adopt batching and retention

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 3

## Prompt (verbatim)

> Adopt both

## How the AI approached it

1. Applied batching:
   - design §5 rule 1 and §6 delivery steps
   - §7 `Notification`/`NotificationItem` contract and batch formats
   - §3 `Delivery.BatchId`
   - §10 tests
   - 02 US-05 acceptance criteria; non-goal wording clarified
2. Applied retention: new design §6a (invariant, table, startup validation,
   test). NFR-08 raised to Must.
3. **Re-budgeted instead of silently absorbing the new scope.** Design
   additions totalled ~50 min against 15–30 min of slack. Build re-estimated
   to 4.15h, buffer down to 0.1h, checkpoint and cut order updated. Sums
   checked with a script (V-031).
4. Cleaned up the remaining stale "no mitigation" text for burst risk in 01.

## Outcome

- D-023, V-030, V-031
- design, requirements, plan and analysis updated
