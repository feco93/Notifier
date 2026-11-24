# 01: Ambiguity Analysis

Phase 1 · 2026-11-24 · Timebox 0.75h

**Inputs:**
- [00-brief.md](00-brief.md)
- Raw AI question list: [../notes/phase1-raw-questions.md](../notes/phase1-raw-questions.md) (38 questions, unfiltered)
- Live USGS feed sample: [../notes/samples/usgs-4.5_day.geojson](../notes/samples/usgs-4.5_day.geojson)

> **Revision 3 (prompt 008):** source confirmed as **BBC News RSS**.
> A-02′…A-04′ now define importance around it.
>
> **Revision 2 (prompt 007, owner review of the [PM] items):**
>
> | Assumption | Change |
> |---|---|
> | A-01 | Now: personal use, hard-coded users |
> | A-05, A-06 | Now: one free multi-topic source |
> | A-07 | Polling confirmed |
> | A-11 | Webhooks confirmed |
> | A-03 | Rule-based importance rejected; the logic will be designed around the chosen source |
>
> - USGS is dropped as the source. The M1–M5 findings in §2 remain as the
>   record of how the first iteration was checked. Their *lessons* (revisions,
>   bursts, identity, ordering) carry over to the new source.
> - The source comparison is in
>   [../notes/source-evaluation.md](../notes/source-evaluation.md).

**Output:** a short list of assumptions with stated reasons that phases 2–3
can build on. Each one is marked as needing PM sign-off where a real PM would
need to confirm it.

---

## 1. Reading the brief: what it actually commits to

| Phrase in brief | What it firmly says | What it leaves open |
|---|---|---|
| "users … set up alerts" | Users configure something themselves; the system doesn't push alerts unprompted | What an alert *is* (rule? subscription?), how expressive |
| "something important happens in the world" | External, real-world events, not app-internal events | Who decides "important"; how it is measured |
| "breaking news, market movements, natural disasters, that kind of thing" | Three example categories; open-ended ("that kind of thing") | Which are in scope; sources; per-category severity |
| "email and Slack" | Two concrete channels required | Slack integration style; email provider |
| "flexible enough … more channels later" | Extensibility is a **design requirement**, not a feature | Runtime plugins vs code-level extension |
| "an admin view too" | A second persona exists with broader visibility | Its contents and powers |

The biggest gap is **"something important"**. It hides three separate
questions, and each one needs its own answer:
1. **Detection:** where do events come from?
2. **Measurement:** how severe is an event, comparably across sources?
3. **Relevance:** is it important *to this user*?

## 2. Critique of the raw AI question list

The raw list was generated first and then reviewed here. Each question is
triaged into one of four buckets:

- 🔴 **Decide now:** blocks the design
- 🟡 **Assume and document:** reasonable default, low regret
- ⚪ **Defer:** out of scope for 6–8h
- ❌ **Drop:** duplicate or not meaningful

| Q | Triage | Comment |
|---|---|---|
| Q01, Q02, Q03, Q05 | 🔴 | The core ambiguity. Merged into **A-02…A-04**. |
| Q04 | 🔴 | Scope driver → A-05 |
| Q06 | ⚪ | Cross-source merging is hard (fuzzy entity matching). MVP has 1 real source, so it can't occur yet. |
| Q07 | 🟡 | A-07 |
| Q08 | ❌ as a question, ⚪ as a feature | **Leading question:** it nudges toward building an LLM classifier, which is the most expensive and least verifiable option. Replaced by an explicit non-goal (§5). |
| Q09 | ⚪ | Only matters once real news sources exist |
| Q10 | 🟡 | Kept, simplified → A-09 |
| Q11, Q12, Q13 | 🔴 | A-05, A-06 |
| Q14 | 🟡 | **Under-weighted by the AI.** Real legal constraint for news. Drives the source choice → A-06 |
| Q15 | 🟡 | A-14 |
| Q16 | ⚪ | No backfill. Only events seen after startup are processed. |
| Q17 | 🔴 | A-08 |
| Q18 | 🔴 | A-09 |
| Q19 | 🟡 | A-10 |
| Q20 | 🟡 | A-12. Digests deferred. |
| Q21, Q22 | ❌ | Not meaningful at this scale / no evidence of need in the brief |
| Q23 | 🟡 | A-08 (enable/disable flag) |
| Q24 | 🟡 | A-11 |
| Q25 | 🔴 | A-11. It changes the auth and setup story significantly. |
| Q26 | 🟡 | One plain template per channel |
| Q27 | ⚪ | Destination verification deferred. A "send test" button covers most of the value. |
| Q28 | 🔴 | A-13. Directly from the brief. |
| Q29 | 🔴 | A-12 |
| Q30 | 🔴 | A-01 |
| Q31 | 🟡 | A-16 |
| Q32, Q33 | 🔴 | A-15 |
| Q34 | ⚪ | The delivery log covers notifications. No general audit log. |
| Q35, Q36 | 🟡 | A-17 |
| Q37 | 🟡 | A-08 (disable) is enough for internal users. A real unsubscribe flow is deferred. |
| Q38 | 🟡 | Source health + delivery log in the admin view = MVP observability |

### Questions the AI list *missed* (found by checking the live USGS feed)

These came from reading real data, not from the brief. That is why the plan
requires real calls.

- **M1. Events change after publication.** In the sample, 5/5 USGS events
  had `updated != time`, and magnitudes get revised. What happens if an event
  first falls *below* a user's threshold and is later revised *above* it?
  What if it is revised down after we already alerted? → A-04
- **M2. Event identity may be unstable (unverified hypothesis).** The AI
  first stated as fact that USGS `ids` lists several network IDs per quake
  and that the preferred `id` can change.
  - **Check:** in the sample, all 5 events had a single ID (e.g.
    `,us6000ud3m,`).
  - **What the format suggests:** the comma-delimited value implies a list
    is *possible*.
  - **What we didn't observe:** aliasing itself.
  - **Treatment:** kept as a low-cost risk, not designed for. → A-04
- **M3. Burst events.** A large quake produces dozens of aftershocks, and one
  rule matching all of them floods a channel. → A-12
- **M4. The PAGER `alert` field is mostly null.** It was null for all
  sampled events, so it can't be the main severity signal. `sig` (0–1000) is
  always present. In the sample, M4.5–5.3 maps to `sig` 312–455, which is
  useful calibration for the severity mapping. → A-03
- **M5. Coordinate order is `[lon, lat, depth]`**, not lat/lon. This is a
  classic source of silent bugs. → noted for implementation and tests.

## 3. Assumptions (the answers we're going with)

Marked **[PM]** where a real PM must confirm. All are revisable via the
decision log.

### Users and context
- **A-01 ✅ owner-decided** The app is for **personal use**, with two roles:
  **user** and **admin**. v1 uses **hard-coded users and one admin**, with
  no registration.
  - *Superseded:* "staff of a single organization" (AI's original guess).
  - *Consequence:* no tenancy, sign-up or user management.

### What "important" means

**Current (revision 3, source = BBC News RSS, prompt 008):**

- **A-02′** An **Article** is the normalized unit:
  - **Identity:** `article_id` = last URL path segment, with the
    `?at_medium=…` query and `#n` fragment stripped. The raw GUID varies per
    feed (V-011).
  - **Fields:** `title`, `summary` (RSS `description`), `url`, `published_at`
    (UTC, from `pubDate`), `first_seen_at`, `topics` (all feeds it appears
    in), `top_stories_rank` (latest position in the Top stories feed, or
    none).
- **A-03′** **Importance = BBC editorial prominence.** We don't score content.
  Each alert picks a tier:
  - **Headline:** article currently in the top N of *Top stories*
    (N configurable, default 5)
  - **Front page:** article appears anywhere in *Top stories*
  - **All:** any new article in the alert's topic(s)

  An optional keyword filter (case-insensitive, title + summary) can narrow
  any tier. *Why:* this delegates "important" to BBC editors, as the owner
  asked (no home-made scoring), and is explainable: "you got this because it
  is #2 on the BBC front page".
- **A-04′** Freshness and updates:
  - An article can only trigger an alert if it was **first seen after the
    alert was created** *and* has `published_at` within a **freshness window**
    (default 24h). This prevents the first-poll flood from old feed items
    (V-011).
  - Rank is **re-evaluated every poll**. An article that later climbs into a
    tier still triggers.
  - Notify **at most once per (article, alert, destination)**.
- **Caveat:** "Top stories order = editorial priority" is inferred from data
  (V-010), not documented by the BBC.

**Superseded (revisions 1–2, USGS/severity based), kept for traceability:**

- ~~**A-02**~~ An **Event** is a normalized record: `source`, `external_id`,
  `category`, `title`, `summary`, `severity` (0–100), `occurred_at` (UTC),
  `updated_at`, optional `location` (lat, lon), `url`, `tags`, `raw` payload.
- **A-03** Importance has two layers, and **no ML in the MVP**:
  1. **Source-specific normalization** maps native signals to a comparable
     0–100 severity. For USGS this comes from `sig`, plus `mag` for display.
     Each mapping is documented and unit-tested.
  2. **User rules** decide relevance: category, minimum severity, optional
     keyword and region.

  *Why:* it is deterministic, explainable to users ("you got this because
  severity 72 ≥ your threshold 60"), testable, and buildable in the budget.
- **A-04** Event identity and updates:
  - Identity = `(source, external_id)`.
  - An event is **re-evaluated on every update**.
  - A user is notified **at most once per (event, alert, channel)**.
  - A later upward revision that newly crosses the threshold *does* notify.
  - Downward revisions after a notification do **not** send a retraction in
    the MVP. This is a documented gap.
  - USGS `ids` aliasing (M2) is a **known risk**, not handled in the MVP.

### Sources
- **A-05 / A-06 ✅ owner-decided (revision 2)**
  - **Exactly one data source**, free, covering several topics.
  - **Chosen: BBC News RSS** (owner-confirmed, prompt 008). Verified live,
    keyless.
  - **Topics in v1:** Top stories, World, Business, Science & Environment,
    Technology.
  - **Terms of use: reviewed (prompt 009). Grey area for personal use;
    licence required for any business use.**
    - The BBC.com terms allow personal, non-commercial use and let
      individuals use the RSS feed with attribution.
    - But they also prohibit automated collection (§7a) and require
      permission for metadata/"computer analysis" (§8a, §12).
    - v1 mitigations: personal use only; forward title + summary + link with
      "Source: BBC News"; no images or logos; poll ≥ TTL; minimal retention;
      **no raw BBC feed data in the public repo**.
    - Full analysis: [../notes/bbc-terms-review.md](../notes/bbc-terms-review.md).
  - The superseded text follows.
- ~~**A-05 [PM]**~~ Category scope for the MVP:
  - **Natural disasters:** real source, USGS earthquakes.
  - **News, markets:** **simulated source only**. Real RSS and market feeds
    are stretch goals.

  *Why:* earthquakes have an objective numeric severity. News "importance"
  is the hardest problem in the brief, and faking it with keyword hacks would
  look like more than it is. Better to show the architecture handles the
  category and be honest about it.
- ~~**A-06** Sources are **free, keyless, and redistributable**. USGS data is
  public domain. The simulated source enables deterministic demos and tests.~~
  *Superseded (found by the phase 2 trace, V-015):*
  - BBC RSS is free and keyless but **not** redistributable (D-013).
  - There is no simulated source any more. Deterministic tests use synthetic
    RSS fixtures, and demos use admin "inject synthetic article" (AS-07).
- **A-07 ✅ owner-confirmed** **Polling** for v1. The interval follows the
  source's declared cadence (BBC RSS declares `ttl=15` min), so expect
  latency of up to roughly 15 min. The earlier "every 2 min, < 5 min" target
  doesn't hold for RSS, and polling faster than the declared TTL would be
  impolite.

### Alerts
- **A-08** An **Alert** is a saved rule owned by a user: name, rule
  criteria, one or more channel destinations, and an enabled flag.
  Pausing = disabling.
- **A-09 (revised, phase 2 trace, V-015)** The rule is a **flat AND**:
  - **topics:** any of the selected topics (OR within topics). *Since Q1
    (prompt 027): topics apply to the All tier only.*
  - **tier:** Headline / Front page / All (A-03′)
  - **keyword:** optional; contained in title or summary, case-insensitive

  No other OR or nesting. Users who need it create two alerts.
  - ~~severity ≥ N~~ and ~~region bounding box~~ were removed: BBC articles
    have no severity or coordinates. These were left over from the USGS
    design.
- **A-10** One alert → **N channel destinations**. Each destination is a
  channel type plus its config (email address, Slack webhook URL).

### Delivery and channels
- **A-11 ✅ owner-confirmed** (webhooks acceptable)
  - **Email** via SMTP. Locally this goes to a mail catcher, so no real
    sending is needed for the demo.
  - **Slack** via **incoming webhooks**, pasted by the user per destination.

  *Why:* a Slack app with OAuth needs a registered app, a public callback URL
  and token storage, which is too much for the budget and not demoable
  offline. Trade-off: webhooks are per-channel and less polished.
- **A-12** Delivery semantics:
  - Immediate dispatch, with **idempotency key = (event, alert, destination)**.
  - **One retry** on failure. Every attempt is logged with status and error.
  - ~~Burst protection (M3) is a stretch goal (per-alert cooldown). Until
    then, the minimum-severity threshold is the mitigation.~~ *Revised
    (V-015):*
    - No severity threshold exists any more. With BBC, the burst risk sits in
      the **All** tier: busy topics like Business carry about 58 items per
      feed.
    - Mitigation: the tier choice itself plus the keyword filter.
    - Cooldowns/digests are out of scope for v1 (02-requirements §5).
    - *Since D-023:* per-poll **batching** groups a cycle's matches into one
      message per alert and destination.
  - No digests or quiet hours.
- **A-13** "More channels later" = **code-level extension**. A new channel is
  one class implementing a `Channel` interface (validate config, send) plus a
  registry entry. No changes to core, rules or UI beyond config fields.
  *Why:* this is the realistic reading of the brief. Runtime-installable
  plugins would be speculative.
- **A-14** A failing source is recorded (last success, last error) and shown
  in the admin view. It does not crash the scheduler or block other sources.

### Admin and access
- **A-15** The **admin view** (operator persona) shows:
  - **Sources:** status, last poll, errors.
  - **Events:** recent ingested events.
  - **Alerts:** every user's alerts (read-only).
  - **Delivery log:** filterable, with failures highlighted.
  - **Test tools:** inject a test event to exercise the pipeline end-to-end.

  Admins **cannot** edit other users' alerts in the MVP (least surprise).
- **A-16** Authentication is **minimal and explicitly non-production**:
  hard-coded users (A-01) with a role flag (`user` / `admin`) and a simple
  login. Documented as a non-goal.
- **A-17** Scale: ≤ 100 users, ≤ a few thousand events/day, single process,
  single machine. This is enough to justify a simple architecture and must
  not be used to justify a distributed one.

### Security minimums
- **A-18** Slack webhook URLs are secrets. They are masked in the UI and logs
  and never committed. Email content escapes event text, so no header
  injection through titles.

## 4. Questions I'd put to the PM first (if one existed)

Ordered by how much the answer would change the design:

1. **Who are the users?** (A-01) Public vs internal changes auth, tenancy,
   unsubscribe flows and the whole admin model.
2. **Who decides "important": us, or each user?** (A-03) Curated global
   importance would need an editorial or ML pipeline. Per-user rules need a
   rule builder.
3. **Which categories matter most at launch, and do we have data
   budgets/licenses?** (A-05, A-06) News licensing alone could block a
   category.
4. **Slack: webhooks acceptable, or must it be a proper Slack app?** (A-11)
5. **What latency counts as "breaking"?** (A-07) Seconds would mean
   push/stream architecture; minutes allows polling.

## 5. Explicit non-goals (6–8h scope)

- ML/LLM-based importance or news classification
- Cross-source event merging (Q06/M2)
- Real news and market feeds (stretch only)
- Digests, quiet hours, cooldowns (cooldown is stretch)
- Production auth, multi-tenancy, destination verification
- Retractions on downward event revisions

## 6. Risks carried forward

| Risk | Impact | Mitigation in MVP |
|---|---|---|
| BBC terms: automated collection (§7a) and metadata/computer analysis (§8a, §12) restricted; Public Service terms (likely governing) unread | **High for production, Medium for personal prototype** | Mitigations 1–8 in `notes/bbc-terms-review.md`; source behind an adapter interface so it can be swapped; production needs a BBC metadata licence |
| Raw BBC feed data published in the public repo | Medium | `bbc-*.xml` git-ignored; synthetic test fixtures |
| "Top stories order = priority" is an inference | Medium | Documented as an assumption; tiers are easy to retune |
| Feed structure changes (e.g. GUID suffixes, malformed dates) | Low | Parser tests on **synthetic** BBC-shaped fixtures (real captures are git-ignored, D-013); lenient date parser (V-021). *Corrected in phase 3: previously said "saved samples", contradicting D-013 (V-025)* |
| ~~Topic × tier intersection misses stories BBC doesn't cross-list~~ | — | Resolved: tier decides, topics only for All (Q1, D-022) |
| At-least-once delivery → rare duplicate after a crash | Low | Unique index + Pending-row resume (03-design §6) |
| Breaking stories in the **All** tier of busy topics → many messages | Low | **Per-poll batching** (D-023): one message per alert per cycle, max 20 items; plus tier + keyword choice |

*Removed in phase 3 (V-025), stale since the switch to BBC:*
- ~~USGS id aliasing~~
- ~~aftershock bursts → severity threshold~~
- ~~low quake volume → simulated source~~
- ~~`sig`→severity mapping~~
