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
