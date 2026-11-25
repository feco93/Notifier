# Prompt 050: Explain the cleanup-guarantee gap

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-25 · **Phase:** 4

## Prompt (verbatim)

> Could you explain the following in depth?
> "A gap in the design's cleanup guarantee"

## Outcome

Explanation in chat:
- the invariant and the assumption it rests on
- the day-by-day failure path
- why future dates are plausible
- the fix, `min(published, firstSeen)`, and rejected alternatives
- how it was verified

The AI also disclosed a residual edge case: a future-dated article that
leaves the feed, is fully purged, and later reappears could be sent once
more. It offered to add this to the risk table (not yet done; no owner
decision).
