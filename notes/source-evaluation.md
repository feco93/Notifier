# Data Source Evaluation (single free, multi-topic source)

2026-11-24 · Prompt 007 · Supports the revisions to A-03, A-05 and A-06

**Requirements (from the user):**
- one source only
- free and easy to use
- covers different topics: news, markets and disasters, the brief's examples
- the importance logic is designed *around* that source

**Method:** live calls where possible. Samples are saved in `notes/samples/`.
Claims I could **not** verify are marked *(memory, unverified)*.

## Candidates

| Source | Verified? | Free / key | Topics | Built-in "importance" signal | Observed problems |
|---|---|---|---|---|---|
| **BBC News RSS** | ✅ live, 5 feeds | Free, **no key** | Top stories, World, Business, Science & Environment, Technology (+ more sections) | **Editorial ranking:** top-stories feed order is not chronological (only 15/32 adjacent pairs newest-first) → position ≈ editors' priority | Old items stay in feeds (back to April); same article in several feeds with different GUID suffix + tracking params; no numeric market/disaster data; UK-centric; ToS *(memory, unverified)*: personal, non-commercial use with attribution, which fits the personal-use scope |
| **GDELT DOC 2.0 API** | ⚠️ partial | Free, no key | Everything, global, multilingual | **Coverage volume spikes** (`timelinevol` worked: 15-min "Volume Intensity" series) | `artlist` returned **HTTP 429 three times**, including a single request after a 15 s pause ("limit requests to one every 5 seconds"). Reliability risk for a polling app; complex query syntax; noisy |
| **The Guardian Open Platform** | ❌ not verifiable | Free, **needs registered key** | Sections (world, business, environment…) | Editors' picks per section *(memory, unverified)* | The AI's recollection that `api-key=test` works was **wrong**: 401 Unauthorized. Would need you to register for a key |
| NewsAPI.org / GNews / NewsData.io | ❌ not checked | Key required; free tiers restricted (delays / request caps / dev-only) *(memory, unverified)* | Categories | None beyond category | Excluded: key + restricted free tiers conflict with "free and easy" |
| USGS earthquakes (previous choice) | ✅ live | Free, no key | Earthquakes only | `sig`, `mag` | Fails "different topics" |

## Recommendation: BBC News RSS

**Why:**
1. It is the only candidate that is **verified working, keyless and
   multi-topic**.
2. It provides an importance signal **without us inventing rules**: BBC
   editors already rank stories. We read their judgement rather than score
   content ourselves. This matches the user's "don't decide importance
   automatically with simple rules" constraint.
3. Personal use fits its terms *(to confirm)*.

**Honest gaps:**
- "Market movements" become *market news*, not price thresholds.
- "Natural disasters" become disaster *coverage*, not sensor severity.
- The editorial view is BBC/UK-centric.

**Runner-up: GDELT.** It has the strongest "something important is
happening" metric (volume spikes), but the 429s observed today make it a poor
basis for a v1 built on polling.

## Draft importance approach for BBC (to be designed properly in phase 3)

- **Topic** = a BBC section feed. The user picks one or more.
- **Importance tier** (the user picks one per alert):
  - **Headline:** article is in the top N positions of the *Top stories* feed
  - **Front page:** article appears anywhere in *Top stories*
  - **All:** any new article in the chosen topic
- **Optional** keyword filter.
- **Re-evaluated on every poll:** a story can climb into the top N later and
  still trigger, but notifies once per (article, alert, destination).

## Pitfalls the design must handle (found in the data, not from memory)

1. **Old items in feeds:** notify only for articles first seen after the
   alert was created *and* with `pubDate` inside a freshness window. Otherwise
   the first poll floods.
2. **Article identity:** key on the article ID from the URL path. Strip the
   `?at_medium=…` query and the `#n` fragment. The raw GUID differs per feed.
3. **Ranking changes between polls:** store the latest rank, not the
   first-seen rank.
4. **Polling cadence:** feeds declare `ttl=15` (minutes). Poll no faster.
