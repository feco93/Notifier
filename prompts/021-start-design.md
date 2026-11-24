# Prompt 021: Start design

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 3

## Prompt (verbatim)

> Start design

## How the AI approached it

1. Wrote `docs/03-design.md`:
   - architecture and domain model (mermaid)
   - matching rules and poll-cycle sequence
   - delivery semantics and channel contract
   - pages/access, auth/config, test strategy
   - slice mapping

   Every element cites the requirement it serves.
2. Wrote ADR 0002 (importance model) and ADR 0003 (channel abstraction).
3. **Checked its own output with scripts**, not by reading:
   - a requirement trace (02 → 03 + ADRs)
   - a stale-term scan over every doc
4. Fixed what the checks found (V-025):
   - 3 requirements without a design
   - 4 stale USGS-era risks in the *live* risk table
   - a risk mitigation contradicting D-013
   - 6 cross-references broken by its own renumbering
5. Listed 5 design choices it made on its own that need owner confirmation
   (§12).

## Outcome

- 03-design.md, ADR 0002, ADR 0003
- D-019, V-025
- **Open:** §12 Q1–Q5
