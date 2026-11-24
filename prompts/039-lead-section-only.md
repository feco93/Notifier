# Prompt 039: Headline ranks the lead section only

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 4

## Prompt (verbatim)

> Lead section only

## Outcome

- `SourceItem` now carries `Section` and per-section `Position`.
- The BBC parser starts a section whenever the GUID marker changes.
- `Article` gains `OnTopStories`; `TopStoriesRank` is lead-section only.
- Fixture and tests reworked; mutation check passed; live re-run with
  section stats.
- Design §3/§4/§4a/§5, ADR 0002, D-025, V-049.
