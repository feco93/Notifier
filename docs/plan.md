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
   admin view > user UI polish > breadth (more sources/channels).

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
| 3 | Design + 3 ADRs, **ending with the stack choice** (D-004) | `03-design.md`, `adr/` | 1h | 2.5h |
| 4 | Build MVP slices, one commit + validation entry each | code + tests | 3.5h | 6h |
| 5 | Verification: E2E demo, independent AI review of core code, screenshots | `notes/` | 0.5h | 6.5h |
| 6 | README, retrospective, transcript export | docs | 0.75h | 7.25h |
|   | Buffer | | 0.75h | 8h |

**Checkpoints (hard stops):**
- **2.5h:** design done. If not, freeze it as-is and start building.
- **5h:** if slice 5 isn't working, drop slice 7 (user UI) and use seeded
  alerts plus the admin view for the demo.
- **6.5h:** stop coding regardless. Remaining time goes to docs and evidence.

## Build slices (phase 4): MVP

| # | Slice | Est. |
|---|---|---|
| 1 | Skeleton, data model, test harness | 0.5h |
| 2 | Simulated source + **one** real source (USGS earthquakes), normalization to `Event` | 0.5h |
| 3 | Rule-matching engine: pure functions, test-first, independently reviewed | 0.5h |
| 4 | `Channel` interface + registry → Email → Slack | 0.75h |
| 5 | Dispatcher: idempotency/dedup + delivery log (single retry, no backoff) | 0.5h |
| 6 | Admin view: delivery log, source status, inject test event | 0.5h |
| 7 | Minimal user view: create/list/delete alerts | 0.25h |

**Stretch (only if ahead of checkpoints):** RSS news + market sources,
retries with backoff, per-rule cooldown, a third channel (webhook) to prove
extensibility, docker-compose, real auth.

**Explicit non-goals for this budget:** ML/NLP-based importance detection,
production auth/multi-tenancy, digests/scheduling, horizontal scaling. These
get documented as future work, not built.

## Initial design hypotheses (to be validated in phases 1–3)

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
