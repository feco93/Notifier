# Plan of Attack

Status: **v2 (2026-11-24)**: re-scoped from the suggested 24h to a **6–8h**
budget (D-005). v1 (24h) was replaced before the first commit; D-005 lists
what was cut. Changes are recorded in
[decision-log.md](decision-log.md), not silently rewritten here.

## Guiding principles

1. **Ambiguity first, code second.** "Something important" is undefined. The
   first hour turns it into explicit, testable assumptions.
2. **Decisions are logged when they're made** (options → choice → why), not
   reconstructed afterwards.
3. **No AI output is accepted unverified.** Each output gets a test, a real
   call against the real API, or an independent review. Rejections go into
   [validation-log.md](validation-log.md).
4. **Vertical slices.** A thin end-to-end path (source → rule → channel) comes
   before breadth.
5. **Priority under time pressure:** process artifacts > working core pipeline >
   Must stories (02 §7) > Should > Could. *Revised with D-014: was "admin view
   > user UI", which contradicted the owner's priorities.*

## Deliverables

| Deliverable | Location | Depth at 6–8h |
|---|---|---|
| Ambiguity analysis + assumptions | `docs/01-ambiguity-analysis.md` | Full: highest leverage |
| Requirements, scope, non-goals | `docs/02-requirements.md` | Concise: stories + acceptance criteria for MVP only |
| Architecture & domain model | `docs/03-design.md` | One diagram, one domain model, key flows |
| ADRs | `docs/adr/` | **3 max:** "important" event model, channel abstraction, stack |
| Decision log | `docs/decision-log.md` | Running |
| Validation log | `docs/validation-log.md` | Running: main evidence |
| Prompt history | `prompts/` | Verbatim + exported transcripts |
| Working implementation + tests | `src/`, `tests/` | MVP (below) |
| Evidence | `notes/` | Screenshots of demo, sample outputs |
| README + retrospective | `README.md`, `docs/retrospective.md` | Short, honest about gaps |

## Phases & timebox

| # | Phase | Output | Budget | Cumulative |
|---|---|---|---|---|
| 0 | Scaffolding | Repo, logs | 0.25h | 0.25h |
| 1 | Brief deconstruction: AI-generated question list, critiqued & pruned, assumed answers | `01-ambiguity-analysis.md` | 0.75h | 1h |
| 2 | Requirements & scope: personas, MVP stories, non-goals | `02-requirements.md` | 0.5h | 1.5h |
| 3 | Design + 3 ADRs. *Stack decided at the start of phase 3 instead of the end, on the owner's stated preferences (D-015, ADR 0001)* | `03-design.md`, `adr/` | 1h | 2.5h |
| 4 | Build MVP slices, one commit + validation entry each. *Re-estimated 3.5h → 4.15h after design additions (D-023)* | code + tests | 4.15h | 6.65h |
| 5 | Verification: E2E demo, independent AI review of core code, screenshots | `notes/` | 0.5h | 7.15h |
| 6 | README, retrospective, transcript export | docs | 0.75h | 7.9h |
|   | Buffer *(was 0.75h; 0.65h consumed by design additions, D-023)* | | 0.1h | 8h |

**Checkpoints (hard stops):**
- **2.5h:** design done. If not, freeze it as-is and start building.
- **5h:** slices 1–4 must be done (was "slice 5 working"; moved because the
  build grew to 4.15h). If behind, apply the pre-agreed cut order in
  [02-requirements.md](02-requirements.md) §7: drop Should/Could first, then
  simplify US-03 and AS-02. *(Was "drop the user UI", which is no longer
  allowed: US-01…05 are Must, D-014.)*
- **6.5h:** stop coding regardless. Remaining time goes to docs and evidence.

## Build slices (phase 4): MVP

| # | Slice | Est. |
|---|---|---|
*Revised for the BBC source (D-011) and owner priorities (D-014). The first
version still named USGS + a simulated source, which was stale.*

| # | Slice | Stories | Est. |
|---|---|---|---|
| 1 | Skeleton, data model, test harness, hard-coded users + login | US-01, AS-01, NFR-12 | 0.5h |
| 2 | BBC source assembly (`Notifier.Sources.Bbc`, internal + `AddBbcNewsSource`): RSS parse, article key, positions, lenient dates; synthetic fixtures | NFR-03, 05, 07, 09 | 0.6h |
| 3 | Tier matcher: pure functions, test-first, independently reviewed | US-05 (matching) | 0.5h |
| 4 | `Channel` interface + registry → Email → Slack (single + **batch** formats); test send | US-04, NFR-02, 06 | 0.9h |
| 5 | Poller + dispatcher: idempotent deliveries, **batching per (alert, destination)**, one retry, **retention purge** | US-05, NFR-01, 04, 08 | 0.85h |
| 6 | User view: create (tier-dependent topics) / list / enable-disable / **soft** delete, test-send button | US-02, 03, 04 | 0.55h |
| 7 | Admin Must: feed status + Poll now | AS-02, AS-06 | 0.25h |
| — | **Should** (if ahead): all-alerts view, delivery-log view, inject synthetic article | AS-04, 05, 07 | — |

**Stretch (Could):** notification history (US-06), recent articles (AS-03),
retry button (AS-08), retention purge (NFR-08), a third channel to show
extensibility, docker-compose.

**Explicit non-goals for this budget:** ML/NLP-based importance detection,
production auth/multi-tenancy, digests/scheduling, horizontal scaling. These
get documented as future work, not built.

## Initial design hypotheses (to be validated in phases 1–3)

> **Superseded.** Kept as the starting point. Phase 1 replaced the
> event/severity/USGS model with BBC editorial-prominence tiers (D-010,
> D-011). Current truth is in `01-ambiguity-analysis.md` (A-02′…A-04′) and
> `02-requirements.md`.

- **"Important"** = normalized `Event` (category, severity, geo, tags, source,
  time) **matched by a user-defined rule** with thresholds. No ML.
- **Sources:** the simulated source gives deterministic demos and tests; USGS
  is the one real feed. Its format gets verified by a real call, not trusted
  from AI output.
- **Pipeline:** ingest (poll) → normalize → dedupe → match → dispatch → log.
- **Channels:** `Channel` interface + registry. Email via SMTP to a local
  catcher; Slack via incoming webhook.
- **Reliability:** idempotency key (event × rule × channel), delivery status.
- **Security (minimum):** webhook URLs treated as secrets, no secrets in
  repo/logs, admin routes separated.

## How AI is directed and checked (scaled to budget)

- Spec-first prompts: each agent gets the relevant doc section + acceptance
  criteria.
- Author/reviewer separation **for core logic only** (matcher, dispatcher).
  Other code gets my own diff review.
- Test-first for the matcher. Tests are spot-checked by breaking the logic and
  confirming they fail.
- Standard checks: hallucinated APIs/fields, timezones, dedup, swallowed
  exceptions, hardcoded values, secrets in logs, scope creep.
- **Every slice ends with a self-review** (owner, D-027), before it is
  reported as finished:
  1. re-read every changed file as if someone else wrote it
  2. list findings with risk
  3. turn testable suspicions into tests, run them *before* fixing
  4. fix
  5. re-run the suite, mutation checks and hygiene scans (comment
     convention, username, unwanted files)
  6. log it
