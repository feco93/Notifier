# BBC Terms of Use: review against our design

2026-11-24 · Prompt 009 · Resolves V-012 (partly)

**Source:** "BBC Global Terms of Use", page updated 15 April 2026, v1.0.
- The owner downloaded it manually, because automated fetching of bbc.co.uk
  was refused.
- The document is **not** copied into this repo, since it is BBC copyright.
- Section numbers below refer to it. Wording is paraphrased; short quotes
  only.

**Not legal advice.** This is an engineering risk assessment to inform scope
decisions.

## 1. Do these terms even apply to the feeds we use?

**Only partly.**
- These are the **BBC.com** terms (BBC Studios, the commercial arm).
- §3 says the **BBC Public Service** properties, explicitly including
  **bbc.co.uk**, are covered by *separate* Public Service terms.
- Our feeds live on `feeds.bbci.co.uk` and link to `www.bbc.co.uk/news/...`,
  so the governing terms are most likely the **Public Service** ones, which
  we still haven't read.
- §8b and §12 themselves route RSS/metadata permission to BBC Public Service.

**Consequence:** treat this document as strong evidence of BBC's stance, not
as the final word.

## 2. What the terms say that affects us

| § | Gist | Effect on our design |
|---|---|---|
| 1 | Services and Content are for **personal, non-commercial use** only | ✅ Matches A-01 (personal use). ❌ Rules out any commercial deployment without a licence |
| 7a | Breach: using robots/scripts to **"collect the Content … using automated means"** | ⚠️ **Direct tension.** Our poller is an automated script. Counterpoint: RSS exists to be consumed by software (feed readers), and §12 treats RSS separately. Still a real ambiguity |
| 8a | Permission needed for **metadata**, and for anything taken from the Services **"to do computer analysis"** | ⚠️ Our "importance tier" reads feed *position* and stores it. That is arguably computer analysis of metadata |
| 12 | Exporting metadata from RSS feeds needs a licence. **Individuals** may add the RSS feed to their site/social account if they don't alter it, credit "BBC News" with a link, and add no BBC branding. **Businesses** need a licence, possibly paid | ✅ Individual use with attribution is the closest allowed case. ❌ Business use needs a licence |
| 7c | Don't pretend to be the BBC: no logos, no copying the look of a service | Notifications must not use BBC logos/styling |
| 5b, 8a | Images/photos need permission | Don't forward `media:thumbnail` images |

## 3. Honest assessment

- **Personal prototype:** defensible but **not clearly permitted**. The
  closest analogue is a personal feed reader that notifies *you* with headline
  + link + attribution. The automated-collection clause (§7a) and the
  "computer analysis" clause (§8a) make it a grey area.
- **Commercial / multi-user product** (what the brief's "users" and "admin
  view" ultimately imply): **needs a BBC Public Service metadata licence**.
  This is a blocker for production, not for the prototype.
- **The AI's earlier memory claim** ("personal non-commercial use with
  attribution is fine") was **half right**. It got the personal/attribution
  part right but missed the automated-collection, metadata and
  computer-analysis restrictions entirely. That is the cost of trusting
  memory.

## 4. Mitigations adopted for v1 (if we stay with BBC)

1. **Personal use only.** Hard-coded users (A-01). Not deployed publicly.
2. **Forward the minimum:** title + RSS summary + link back to bbc.co.uk.
   No images, no full article text (we never fetch article pages).
3. **Attribution on every notification:** "Source: BBC News" with a link.
4. **No BBC logos or styling** in emails, Slack or UI.
5. **Polite polling:** no faster than the feed `ttl` (15 min). Identify the
   client honestly in the User-Agent.
6. **Retention:** keep only what dedup and ranking need (article ID, title,
   link, timestamps, rank). Purge after the freshness window + margin.
7. **No raw BBC feed content in the public GitHub repo.** Committing the
   saved XML samples would arguably be exporting/republishing feed metadata
   (§12). They are git-ignored. Tests use **synthetic fixtures** in the same
   RSS shape.
8. **Documented production prerequisite:** a BBC Public Service metadata
   licence, *or* swap the source adapter. The source sits behind an
   interface, so this is a contained change.

## 5. Still open

- The **BBC Public Service** terms (the ones most likely governing
  `feeds.bbci.co.uk`) have not been read.
- The page §12 links to about RSS/metadata licensing has not been read.
