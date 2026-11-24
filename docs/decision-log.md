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
