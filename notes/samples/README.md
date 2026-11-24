# Samples

Raw API responses captured during source evaluation (phase 1, 2026-11-24).

| File | Committed? | Why |
|---|---|---|
| `usgs-4.5_day.geojson` | ✅ | USGS data is public domain |
| `gdelt-timelinevol.json` | ✅ | Numeric volume series only, no article content |
| `gdelt-artlist.json` | ✅ | Contains only the HTTP 429 error body (evidence of rate limiting) |
| `guardian-search.json` | ✅ | Contains only the 401 error body |
| `bbc-*.xml` (5 feeds) | ❌ git-ignored | BBC terms restrict exporting RSS metadata. See [../bbc-terms-review.md](../bbc-terms-review.md) |

The structural findings from the BBC captures are recorded in the validation
log (V-010, V-011) and [../source-evaluation.md](../source-evaluation.md):
- ordering
- item age
- cross-feed overlap
- GUID suffixes
