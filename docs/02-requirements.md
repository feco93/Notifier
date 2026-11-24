# 02: Requirements & Scope (v1)

Phase 2 · 2026-11-24 · Timebox 0.5h

**Basis:** assumptions in [01-ambiguity-analysis.md](01-ambiguity-analysis.md)
(revision 3) and decisions D-009…D-013.

**Priority (MoSCoW)** is aligned with the plan's checkpoints:
- **Must:** needed for the 5h checkpoint demo
- **Should:** before 6.5h
- **Could:** stretch

## 1. Personas

| Persona | Who | Goal | Access |
|---|---|---|---|
| **User** | The owner (personal use, A-01). Hard-coded account(s) | Hear about important BBC stories on chosen topics, by email or Slack, without checking the news | Own alerts only |
| **Admin** | Hard-coded admin account | Check that the system works: feeds polled, alerts firing, deliveries succeeding; diagnose failures | Read-only view of everything + operational actions |

## 2. Core concepts (glossary)

| Term | Meaning |
|---|---|
| **Topic** | A BBC section feed: Top stories, World, Business, Science & Environment, Technology |
| **Article** | A normalized BBC item, identified by its URL article ID (A-02′) |
| **Tier** | Importance level of an alert (A-03′): **Headline** = article in top N (default 5) of Top stories; **Front page** = article anywhere in Top stories; **All** = any new article in the alert's topics |
| **Alert** | A user's saved subscription: topics + tier + optional keyword + ≥1 destination + enabled flag |
| **Destination** | Channel type + its config: an email address, or a Slack incoming-webhook URL |
| **Delivery** | One attempt to send one article to one destination for one alert, with status |

## 3. User stories & acceptance criteria

### User

**US-01 · Log in** · Must
> As a user I log in so that I see and manage only my alerts.
- Valid hard-coded credentials → my alert list. Invalid → error message, no
  session.
- A user requesting any admin page gets **403**.
- Log out ends the session.

**US-02 · Create an alert** · Must
> As a user I create an alert so that I'm notified about stories I care about.
- Fields:
  - name (required)
  - topics (≥ 1 of the 5)
  - tier (Headline / Front page / All)
  - keyword (optional)
  - destinations (≥ 1)
- A destination is either an email address (format validated) or a Slack
  webhook URL. It must be `https://hooks.slack.com/...`; any other host is
  rejected (SSRF guard).
- Validation errors are shown per field. Nothing is saved on error.
- `created_at` is stored. Only articles **first seen after** this moment can
  trigger it (A-04′).

**US-03 · Manage my alerts** · Must
> As a user I list, enable/disable and delete my alerts.
- The list shows name, topics, tier, keyword, destinations (webhook URLs
  **masked**) and the enabled state.
- A disabled alert never fires. Re-enabling doesn't backfill articles missed
  while it was disabled.
- Delete asks for confirmation and removes the alert's future deliveries.
  Past delivery log rows are kept for the admin.
- A user can't see or modify another user's alerts (direct URL → 404).

**US-04 · Send a test notification** · Must *(raised from Should, D-014)*
> As a user I send a test message to a destination so that I know it's set
> up correctly.
- Sends a clearly marked "TEST" message through the real channel.
- The UI shows success, or the channel's error message.

**US-05 · Receive a notification** · Must
> As a user I get a message when an article matches my alert.
- **Matching:**
  - Fires when an enabled alert's topic and tier match (tier semantics in §2).
  - With a keyword, the keyword must also appear in the title or summary
    (case-insensitive).
- **Eligibility:**
  - The article was first seen after the alert was created.
  - Its `published_at` is within the **freshness window** (default 24h).
- **Rank changes:** an article that **later** enters the tier (e.g. climbs
  into the top 5) fires on the poll where it qualifies.
- **At most once** per (article, alert, destination), across polls and
  restarts.
- **Content:**
  - BBC title, RSS summary, link to the article
  - **"Source: BBC News"** attribution
  - **why it fired** (e.g. "#2 on BBC Top stories", "New in Technology",
    "keyword: 'nvidia'")
  - No images, no BBC logos (D-013).

**US-06 · My notification history** · Could
- The user sees their own recent deliveries (article, alert, channel, status).

### Admin

**AS-01 · Admin access** · Must
- Admin logs in with the hard-coded admin account. Admin pages are reachable
  only by the admin role.

**AS-02 · Feed status** · Must
- Per topic feed:
  - last poll time (UTC)
  - last result (ok / error + message)
  - items in the last fetch
  - new articles in the last fetch
  - next scheduled poll
- A failing feed doesn't stop the other feeds (A-14).

**AS-03 · Recent articles** · Could *(lowered from Should, D-014)*
- Latest articles with topics, current Top-stories rank, published time and
  first-seen time.

**AS-04 · All alerts** · Should *(lowered from Must, D-014)*
- Every user's alerts, read-only, webhook URLs masked.

**AS-05 · Delivery log view** · Should *(lowered from Must, D-014)*
- Note: **recording** every attempt stays Must (NFR-04). Only the admin
  *view* of the log moved to Should.
- Shows every delivery attempt: time, user, alert, article, channel, status
  (sent / failed / test), error message.
- Filter by status. Failures are visually highlighted.

**AS-06 · Poll now** · Must
- The admin triggers an immediate poll of all feeds, so the demo doesn't wait
  15 min.
- It respects a minimum interval between manual polls (≥ 60 s) to stay
  polite to the BBC.

**AS-07 · Inject a synthetic article** · Should
- The admin injects a test article (topic, rank, title) to exercise
  matching → delivery end-to-end, independently of live news.
- Marked **SYNTHETIC** in notifications and logs.

**AS-08 · Retry a failed delivery** · Could

## 4. Non-functional requirements

| ID | Requirement | Pri | From |
|---|---|---|---|
| NFR-01 | **Polling:** interval configurable, default and minimum = feed `ttl` (15 min) for live BBC | Must | A-07, D-012, D-013 |
| NFR-02 | **Extensibility:** adding a channel = one new class implementing the channel interface + a registry entry. **No changes** to matching, dispatch or the alert model. Demonstrated by a test or documented walkthrough | Must | Brief, A-13 |
| NFR-03 | **Source isolation:** BBC-specific parsing sits behind a source adapter interface, so the source can be swapped (licence risk) | Must | D-013 |
| NFR-04 | **Reliability:** idempotent delivery (unique key on article × alert × destination); **one retry** on failure; every attempt logged | Must | A-12 |
| NFR-05 | **Time:** all timestamps stored in UTC; feed `pubDate` parsed with its timezone | Must | M-lessons |
| NFR-06 | **Secrets:** webhook URLs and SMTP credentials never logged or shown unmasked; no secrets in the repo; config via environment | Must | A-18 |
| NFR-07 | **Compliance:** forward only title + summary + link with attribution; no images or logos; honest User-Agent; no raw BBC data in the repo | Must | D-013 |
| NFR-08 | **Retention:** articles older than freshness window + 7 days are purged | Could | D-013 |
| NFR-09 | **Testability:** feed parser, article identity, tier matching and dedup covered by automated tests using **synthetic** RSS fixtures | Must | V-014 |
| NFR-10 | **Runnability:** one documented command starts the app locally; email goes to a local mail catcher; Slack works with a real webhook if provided | Must | Plan |
| NFR-11 | **Scale:** single process, single machine; a handful of users | Must | A-17 |
| NFR-12 | **Auth:** non-production; hard-coded users with dev-only credentials from config | Must | A-01, A-16 |

## 5. Out of scope for v1 (non-goals)

- Registration, password reset, user management UI, multi-tenancy
- Sources other than BBC News RSS; cross-source merging
- Content-based importance scoring (ML, LLM or home-made rules): explicitly
  rejected by the owner (D-010)
- Digests, quiet hours, cooldowns, retractions when an article drops out of a
  tier
- Destination verification (US-04's test message is the substitute)
- Admin editing users' alerts
- Production deployment, commercial use (would need a BBC licence)

## 6. Traceability (assumption → requirement)

Checked with a script that lists every A-xx in `01-ambiguity-analysis.md`
and looks for it here (V-015). Gaps found by that check were fixed in 01, not
papered over here.

| Assumption | Covered by |
|---|---|
| A-01 personal use, roles | US-01, AS-01, NFR-12 |
| A-02′ article identity | US-05, NFR-09 |
| A-03′ tiers | §2 glossary, US-02, US-05 |
| A-04′ freshness / once-only | US-02, US-05, NFR-04 |
| A-05, A-06 BBC single source | §2 topics, NFR-03, NFR-07 |
| A-07 polling | NFR-01, AS-02, AS-06 |
| A-08 alert = saved rule, disable | US-02, US-03 |
| A-09 flat AND (revised) | US-02, US-05 |
| A-10 alert → N destinations | US-02 |
| A-11 email SMTP + Slack webhook | US-02, US-04, US-05, NFR-10 |
| A-12 idempotency, one retry | NFR-04, AS-05 |
| A-13 channel extensibility | NFR-02 |
| A-14 failing feed isolated | AS-02 |
| A-15 admin view contents | AS-02…AS-07 |
| A-16 minimal auth | US-01, NFR-12 |
| A-17 scale | NFR-11 |
| A-18 secrets | US-02 (SSRF guard), US-03, AS-04, NFR-06 |

## 7. Scope vs budget check

| Priority | Stories / NFRs |
|---|---|
| **Must** | US-01, 02, 03, 04, 05; AS-01, 02, 06; NFR-01…07, 09…12 |
| **Should** | AS-04, AS-05, AS-07 |
| **Could** | US-06, AS-03, AS-08, NFR-08 |

*Revised by owner (D-014):* US-04 raised; AS-04, AS-05 and AS-03 lowered.

- **Budget:** the Must set shrank. Two admin screens (all alerts, delivery
  log) became Should, and one small user action (test send) became Must. That
  leaves about 0.25–0.5h of slack in the 3.5h build.
- **How Must delivery is verified without the log view:**
  - US-04 test sends, plus real arrival in the mail catcher / Slack
    channel.
  - Delivery records are still written (NFR-04) and checked by automated
    tests.
- **Pre-agreed cuts if behind at the 5h checkpoint**, in this order:
  1. drop all Should/Could
  2. simplify US-03 to enable/disable only (no delete)
  3. AS-02 shows only last poll + status
