# Prompt 049: Start slice 5 (poller, dispatcher, retention)

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-25 · **Phase:** 4

## Prompt (verbatim)

> Start slice 5

## How the AI approached it

1. Re-read design §5/§6/§6a first. Found a hole in the retention invariant
   (future-dated articles, V-061) and a missing `Delivery.Reason`.
2. Wrote 29 integration tests first, over a harness with in-memory SQLite,
   a scripted source, a recording channel and a fake clock. Red (no compile).
3. Implemented:
   - `PollService` (sequential fetch, upsert, lead-section ranks, failure
     isolation)
   - `Dispatcher` (pending rows, batching with cap, rank order, one retry,
     crash resume)
   - `RetentionService`
   - `PollingWorker` + `PollTrigger`
   - options validation
4. **Self-review (standing rule, D-027):**
   - treated the all-green first run as unproven
   - 18 mutations: 3 weak tests found and strengthened
   - fixed back-to-back polling after manual requests
5. Ran one live cycle against the real BBC feeds.

## Outcome

- 199 tests + 1 gated E2E
- V-061…V-064
- design §3/§4/§5/§6 updated
