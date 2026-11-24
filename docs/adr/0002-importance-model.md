# 0002: Importance model: BBC editorial prominence tiers

Status: **accepted** (D-010, D-011, D-022). Topic/tier semantics decided in
prompt 027.
Date: 2026-11-24

## Context

The brief's "something important" is undefined. The owner ruled out
content-based scoring (ML, LLM or home-made rules) and asked for importance to
be derived from the single chosen source (D-010). The source is BBC News RSS
(D-011).

Observed in real data (phase 1):
- The Top stories feed is **not chronological** (15/32 adjacent pairs
  newest-first), which implies an editorial ranking (V-010).
- Feeds keep old items, back to April (V-011).
- Articles are cross-listed across feeds with differing GUIDs (V-011).
- `pubDate` can be missing or malformed, and the default parser then fails
  the whole feed (V-021).

## Options considered

| Option | Verdict |
|---|---|
| Content scoring: ML/LLM or keyword weights | ❌ Rejected by owner (D-010); unverifiable in budget |
| Coverage volume spikes (GDELT) | ❌ Source rejected: repeated HTTP 429 (V-009) |
| Cross-listing count (in N feeds ⇒ important) | ❌ as the primary signal: weak and unexplainable. Could add later |
| **Editorial prominence:** position in BBC Top stories | ✅ Uses BBC editors' judgement, explainable ("#2 on BBC Top stories"), no home-made scoring |

## Decision

- Each alert picks a **tier**:
  - **Headline:** rank ≤ N (default 5)
  - **Front page:** any rank
  - **All:** no prominence condition
- It also picks **topics** and an optional **keyword**.
- **The tier decides:** Headline/Front page ignore topics; topics apply to
  All only. *(The intersection default was refuted by data: 2/5 headlines
  were cross-listed, V-029.)*

**Eligibility rules:**
- first seen after the alert was created
- published within 24h, with a known date
- ranks only update after a successful Top stories fetch
- notify once per (article, alert, destination)

Full rules: [03-design.md §4–§5](../03-design.md).

## Consequences

- **+** Explainable notifications. A pure, table-testable matcher.
- **+** No tuning knobs beyond N and the freshness window.
- **−** "Top stories order = priority" is an **inference**, not documented BBC
  behaviour. If wrong, tiers become noisy but still work as filters.
- **−** There's no "important stories *in topic X*" except via keyword.
  ~~Intersection semantics can miss a story that BBC doesn't cross-list
  into the chosen topic feed.~~ (avoided by the Q1 decision)
- **−** "Market movements" and "natural disasters" become *news coverage* of
  them, not market data or sensor severity (stated in A-05).

## How the AI was involved / what was challenged

- The AI's first model (USGS severity + user rules) was rejected by the owner
  (prompt 007).
- The editorial-ranking hypothesis was tested against data before being
  proposed (V-010).
- The (later rejected) baseline-poll and the rank-preservation rules came from reasoning about
  failure modes during design. They have not yet been tested; slice 5
  integration tests are where they get verified.
