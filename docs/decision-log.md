# Decision Log

Running, append-only log. Significant decisions get a full ADR in `adr/`.
Format: **ID · date · decision** — options considered → choice → why.

---

### D-001 · 2026-11-24 · Process-first repository layout
- **Options:** (a) code-first repo, write docs at the end; (b) docs/prompts/logs
  scaffolded before any code.
- **Choice:** (b).
- **Why:** The assignment evaluates the process. Reconstructing reasoning
  afterwards is lossy and less honest.

### D-002 · 2026-11-24 · Plan structure: phased, vertical-slice build
- **Options:** (a) build features layer by layer (all models, then all
  services, then UI); (b) thin end-to-end slices.
- **Choice:** (b).
- **Why:** Always have a demonstrable system; partial delivery still works
  end-to-end; each slice is a natural milestone commit.

### D-003 · 2026-11-24 · Stack — ~~PROPOSED~~ superseded by D-004
- **Proposal (AI, in prompt 001):** Python + FastAPI, SQLite + SQLAlchemy,
  APScheduler, Jinja + HTMX for UI/admin, Mailpit via docker-compose.
- **Environment check:** Python 3.15.2, Docker 29.9.3, Git 2.56.1 available
  locally (verified, not assumed).
- **Status:** Withdrawn — see D-004.

### D-004 · 2026-11-24 · Defer the stack decision until after design
- **Context:** The AI proposed a stack in the very first planning answer,
  before any requirement was known. I challenged this (prompt 003).
- **Options:** (a) fix the stack now to unblock scaffolding; (b) decide it
  as the last step of phase 3, once features, NFRs and the architecture are
  known.
- **Choice:** (b).
- **Why:** The stack should be derived from requirements — e.g. need for
  real-time UI, background-job load, number of sources/channels, deployment
  target — not the other way round. Nothing in phases 1–3 depends on it.
  Choosing early would anchor the design on an arbitrary pick.
- **Consequence:** Phases 1–3 stay technology-neutral. Stack gets its own ADR
  with requirements-based selection criteria.

### D-005 · 2026-11-24 · Re-scope plan from 24h to 6–8h
- **Context:** Real available time is 6–8h, not the suggested 24h (prompt 004).
- **Options:** (a) keep scope, work faster and thinner everywhere;
  (b) keep process depth, cut build breadth; (c) cut process docs, maximize
  code.
- **Choice:** (b).
- **Why:** The assignment evaluates process over output ("partial but
  well-reasoned beats complete but undocumented"). Option (c) optimizes the
  wrong thing; option (a) yields shallow everything.
- **What was cut vs v1:**
  - Real sources 3 → 1 (USGS) + simulated
  - Retries with backoff, cooldowns, third channel, docker-compose → stretch
  - ADRs capped at 3
  - Author/reviewer agent split only for core logic
  - Total 24h → ~7.25h + 0.75h buffer
- **Added:** hard checkpoints at 2.5h / 5h / 6.5h with pre-decided cut actions,
  and an explicit priority order (process > core pipeline > admin > user UI >
  breadth).

### D-006 · 2026-11-24 · ~~"Important" = normalized severity + user rules, no ML~~ superseded by D-010
- **Options:**
  - (a) LLM/ML classifier judges importance
  - (b) curated global importance set by admins
  - (c) per-source severity normalization (0–100) + per-user rules
- **Choice:** (c). Detail: A-02…A-04 in `01-ambiguity-analysis.md`.
- **Why:** deterministic, explainable to the user, unit-testable, fits the
  budget. (a) is unverifiable within 6–8h. (b) needs an editorial role the
  brief doesn't mention.
- **Follow-up:** formal ADR in phase 3.

### D-007 · 2026-11-24 · ~~MVP categories: real earthquakes, simulated news/markets~~ superseded by D-011
- **Choice:** USGS earthquakes as the single real source. News and markets go
  through a simulated source.
- **Why:**
  - Earthquakes have an objective numeric severity, verified in the live
    sample (`sig`, `mag`).
  - Keyword-based "breaking news" detection would overstate what's built.
  - Simulated categories still prove the pipeline is category-agnostic.
- **Needs PM sign-off** (A-05).

### D-008 · 2026-11-24 · Slack via incoming webhooks, not a Slack app
- **Why:** an OAuth Slack app needs app registration, a public callback and
  token storage, which is too much for the budget and can't be demoed offline.
  Webhooks are a pasted URL. Trade-off accepted: less polished, per-channel.
- **Owner-confirmed** in prompt 007.

### D-009 · 2026-11-24 · Personal use, hard-coded user + admin (owner decision)
- **Context:** the AI assumed an org-internal, single-tenant tool (A-01). The
  owner overrode it.
- **Choice:** personal use; roles `user` and `admin`; v1 users hard-coded.
- **Consequence:** no registration, user management or tenancy. Auth stays
  minimal (A-16).

### D-010 · 2026-11-24 · Importance comes from the source, not from our rules (owner decision)
- **Context:** the owner rejected AI-designed severity scoring/rules as
  overcomplicated (prompt 007).
- **Choice:** pick one source first, then design importance around what that
  source natively provides.
- **Draft:** for BBC RSS, importance = **editorial prominence**, i.e.
  position in the Top-stories feed. Tiers: Headline / Front page / All. See
  `notes/source-evaluation.md`.
- **Status:** assumption-level definition fixed in A-02′…A-04′ (prompt 008).
  Detailed design and formal ADR come in phase 3.

### D-011 · 2026-11-24 · One free, multi-topic source; BBC News RSS recommended
- **Options evaluated with live calls:**
  - BBC RSS: works, keyless
  - GDELT: repeated HTTP 429
  - Guardian: needs a registered key; the "test" key returned 401
  - NewsAPI-style services: key and free-tier limits, not verified
  - USGS: one topic only
- **Recommendation:** BBC News RSS. Runner-up: GDELT (best importance metric,
  unreliable access observed).
- **Status:** ✅ **accepted by owner** (prompt 008).
- **v1 topic feeds:** Top stories, World, Business, Science & Environment,
  Technology.
- **Open item:** BBC terms of use are unverified (V-012).

### D-012 · 2026-11-24 · Polling at the source's declared cadence (owner-confirmed)
- **Choice:** poll; interval ≥ feed `ttl` (15 min for BBC).
- **Consequence:** the AI's earlier "< 5 min latency" target is dropped, since
  it was set before the source was known.

### D-013 · 2026-11-24 · BBC terms compliance posture for v1
- **Context:** the owner supplied the BBC Global Terms of Use (prompt 009).
  - Personal, non-commercial use is allowed.
  - Individuals may use the RSS feed with attribution.
  - Automated collection, metadata export and "computer analysis" need
    permission.
  - Business use needs a licence.
- **Options:**
  - (a) keep BBC with mitigations as a personal prototype
  - (b) switch source now (back to the runners-up: GDELT unreliable, Guardian
    needs a key)
  - (c) seek a BBC licence (not feasible in the timeframe)
- **Choice:** (a), ✅ **accepted by owner** (prompt 010). Mitigations:
  - personal use only
  - forward title + summary + link with attribution; no images or logos
  - poll ≥ TTL
  - minimal retention
  - no raw feed data in the repo
  - source behind an adapter interface
- **Why:** the prototype's purpose is to demonstrate the alerting
  architecture. Keeping the source swappable limits the cost of a later
  licence or source change. The production prerequisite is documented
  explicitly rather than ignored.

### D-014 · 2026-11-24 · Owner re-prioritization of requirements
- **Change (prompt 013):**
  - US-04 test notification: Should → **Must**
  - AS-04 all-alerts view: Must → **Should**
  - AS-05 delivery-log view: Must → **Should**
  - AS-03 recent articles: Should → **Could**
- **Effect:** the Must scope shifts from admin screens toward the user's own
  workflow. Build slack rises to about 0.25–0.5h.
- **Clarification recorded:** *recording* delivery attempts stays Must
  (NFR-04). Only the admin *view* moved to Should.
- **Knock-on fixes in `plan.md`:**
  - the priority order (was "admin view > user UI")
  - the 5h checkpoint (was "drop the user UI", now forbidden)
  - build slices, which still named USGS + a simulated source (stale since
    D-011)

### D-015 · 2026-11-24 · Stack: ASP.NET Core Razor Pages on .NET 10 LTS → [ADR 0001](adr/0001-tech-stack.md)
- **Owner preferences:** C#/TypeScript, ASP.NET LTS/React (prompt 015).
- **AI analysis:** an API + React SPA costs about 30–45 min more (two
  pipelines, proxy/CORS, SPA auth), which consumes all build slack.
- **Owner choice (prompts 016–017):** reduce cost and use **Razor Pages**.
  React/TypeScript are dropped.
- **Timing note:** decided at the *start* of phase 3 instead of the end
  (plan v2). This still honours D-004's intent: requirements existed first.

### D-016 · 2026-11-24 · Local email catcher: smtp4dev (fallback Mailpit)
- **Why:** installs through the `dotnet` CLI (no Docker), has a web UI for
  evidence screenshots, supports NFR-10.
- **Pre-decided fallback:** if smtp4dev doesn't run on the installed runtimes,
  use the Mailpit standalone binary.

### D-017 · 2026-11-24 · Remaining stack defaults (AI-proposed, owner did not object)
- SQLite + EF Core
- Cookie auth with config-defined users
- `BackgroundService` + `PeriodicTimer`
- `TimeProvider`
- Syndication RSS parser
- MailKit
- xUnit
- three-project layout

Items resting on model memory are marked **to verify** in ADR 0001.

### D-018 · 2026-11-24 · Verified library choices (prompt 018)
- **RSS:** `System.ServiceModel.Syndication` 10.0.15 with a lenient
  `DateTimeParser`. Alternative `XmlReader` parser not needed.
- **Email:** MailKit 5.0.0, targeting smtp4dev locally (both verified end to
  end).
- **Tests:** xUnit v3. FluentAssertions 8.x excluded (licence).
- **Assertion style:** plain xUnit `Assert` (owner, prompt 019). No extra
  dependency; Shouldly and AwesomeAssertions were the alternatives.

### D-019 · 2026-11-24 · Design v1: single process, editorial tiers, self-describing channels
- Design: [03-design.md](03-design.md).
- ADRs: [0002 importance](adr/0002-importance-model.md),
  [0003 channels](adr/0003-channel-abstraction.md).
- **AI-introduced rules, each from a failure mode identified while
  designing:**
  - baseline poll per feed (otherwise the first start floods alerts)
  - ranks only change after a successful Top stories fetch
  - unknown `pubDate` ⇒ not eligible
  - Pending-row resume ⇒ at-least-once delivery
  - `ConfigFields` on channels, so the destination form doesn't need a page
    change per channel (NFR-02)
- **Open owner questions:** 03-design §12 Q1–Q5.

### D-020 · 2026-11-24 · Two projects kept; source behind `IArticleSource` (owner chose option A)
- **Context:** the owner asked why Core/Web exist (prompt 022). Answering
  exposed that NFR-03 wasn't actually met (V-026).
- **Options:**
  - **A.** two projects + source interface
  - **B.** single project + interface
  - **C.** three projects (Core / Infrastructure / Web)
- **Choice:** A (prompt 023).
- **Why:** a compiler-enforced boundary for the matcher/domain at ~5 min cost.
  C adds ceremony a 3.5h build can't justify.
- **Consequences:**
  - Core is BCL-only.
  - All BBC specifics live in `Web/Sources/Bbc`.
  - BBC parser tests move to the Web side.
  - The poll-cycle integration test uses a fake `IArticleSource`, which also
    demonstrates NFR-03.
  - Stated limitation: the Headline / Front page tiers require any
    replacement source to provide a ranked TopStories list.

### D-021 · 2026-11-24 · BBC source in its own assembly `Notifier.Sources.Bbc` (owner-raised)
- **Context:** the owner asked whether the BBC source should be a separate
  assembly (prompt 024). The AI's earlier options had skipped this narrow
  variant (V-028).
- **Choice:** four projects: Core, Sources.Bbc, Web, Tests.
  - BBC types are `internal`.
  - The only public surface is `AddBbcNewsSource()`.
  - Tests use `InternalsVisibleTo`.
- **Why:**
  - a compiler-enforced encapsulation of BBC specifics
  - the Syndication dependency leaves the host
  - NFR-03 swap = a project reference + one line
  - ~5–10 min cost
- **Deliberate asymmetry:** channels stay in Web. NFR-02 needs *adding*
  channels, not swapping them, and there's no channel-side equivalent of the
  BBC licence risk.

### D-022 · 2026-11-24 · Owner decisions on design Q1–Q5 (prompt 027)
- **Q1 ✅** Tier decides. Headline/Front page ignore topics; topics apply to
  the All tier only. Keyword narrows any tier.
- **Q2 ✅** ≤ 1 destination per channel per alert.
- **Q3 ✅** No edit in v1. **Soft delete** (`Alert.DeletedAtUtc`) keeps
  delivery history.
- **Q4 ✅** At-least-once delivery after a crash.
- **Q5 ❌** Baseline poll rejected. The owner proposed batching several
  matches into one message instead. Batching and a retention policy are being
  analysed (D-023, pending).

### D-023 · 2026-11-24 · Per-poll batching (owner) + retention policy (NFR-08 → Must)
- **Batching (owner-proposed, replaces the rejected Q5 baseline):**
  - Matches from one poll cycle are grouped per (alert, destination) into
    one message, max 20 items + "…and N more".
  - `Delivery` rows stay per article (once-only unchanged) and gain a
    `BatchId`.
  - `Notification` becomes a list of items.
  - Not a timed digest. Digests remain a non-goal.
- **Retention (owner asked; AI's design had 3 flaws, V-030):**
  - Invariant: delete only what can no longer match.
  - Deliveries are kept 30 d.
  - Articles are deleted when not seen for freshness + 7 d and no deliveries
    remain.
  - Soft-deleted alerts are removed after their history.
  - Startup validation enforces the invariant.
  - NFR-08 Could → **Must** (D-013 consistency).
- **Budget:** build 3.5h → 4.15h. Buffer 0.75h → 0.1h (V-031).

### D-024 · 2026-11-24 · Code comment convention (owner, prompt 032)
- **Rule:** comments only state a short, non-obvious *why*: a security
  choice, a framework gotcha or an invariant.
- **Not allowed in code:**
  - decision, validation or requirement IDs
  - design-doc section references
  - "found in review" or other process history
  - comments that restate the code
  - requirement IDs in user-visible UI text
- **Why:** the traceability trail lives in these logs. Code should read
  cleanly on its own.
- **Applied:** all slice 1 files trimmed. A grep for process references in
  `src/` and `tests/` finds none.

### D-025 · 2026-11-24 · Headline ranks only the lead section of Top stories (owner, prompt 039)
- **Context:** the owner challenged the duplicate explanation (prompt 038).
  The data showed that each BBC feed concatenates page modules, with the
  GUID `#n` marking the module (V-048). Raw position is only a ranking within
  the lead module.
- **Options:**
  - **Lead section only:** the source reports `Section` + `Position` within
    the section; only section 0 gets a rank
  - **Keep raw position:** document the risk
- **Choice:** lead section only.
- **Consequences:**
  - `SourceItem` gains `Section`, and `Position` becomes per-section.
  - `Article` gains `OnTopStories` (Front page = any section), and
    `TopStoriesRank` = lead-section rank only.
  - A section starts whenever the marker changes. Items without a marker stay
    in the current section, so a feed with no markers degrades to a single
    section (today's behaviour).
  - Schema change: delete the local `notifier.db` (`EnsureCreated`).

### D-026 · 2026-11-25 · Keywords match whole words or phrases (owner, prompt 042)
- **Context:** the spec said "contained in" (substring). Measured on the
  captured feeds, "AI" hit 60 items as a substring vs 14 as a word, and
  "art" hit 13 vs 0 (V-051).
- **Choice:** whole word or phrase.
  - case-insensitive
  - a word boundary on both ends
  - any whitespace between phrase words
  - regex-escaped, so "C++" and "U.S." work literally
- **Trade-off accepted:** no inflection matching ("war" ≠ "wars"). Users
  add a second alert for other forms.
