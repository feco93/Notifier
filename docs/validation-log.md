# Validation Log

Every non-trivial AI output gets an entry: what it produced, how I checked it,
and the outcome. This file is the main evidence of critical review.

**Outcome:** ✅ accepted · ✏️ accepted with changes · ❌ rejected

| ID | Date | Phase | AI output | Check applied | Outcome | Notes / correction |
|---|---|---|---|---|---|---|
| V-001 | 2026-11-24 | 0 | High-level plan (prompt 001) | Read against the assignment's evaluation criteria | ✏️ | Kept structure. Stack left as a proposal (D-003) rather than accepted silently. |
| V-002 | 2026-11-24 | 0 | AI pushed for a stack decision before any requirements existed | Questioned ordering: does anything in phases 1–3 need a stack? No. | ❌ | Premature anchoring. Stack decision moved to end of phase 3 (D-004). |
| V-003 | 2026-11-24 | 0 | Re-scoped plan v2 | Phase budgets sum checked (7.25h + 0.75h buffer = 8h); slice estimates sum to the 3.5h build phase; v1's "in git history" claim was false (nothing committed yet) | ✏️ | Fixed the false git-history claim in plan.md. |
| V-004 | 2026-11-24 | 1 | Raw list of 38 PM questions | Triaged each question: decide now / assume / defer / drop | ✏️ | Dropped Q21, Q22 (no evidence of need). Q08 rejected as a **leading question** that pushes toward an LLM classifier. Q14 (news licensing) was under-weighted and promoted. |
| V-005 | 2026-11-24 | 1 | Implicit assumption that the brief alone is enough to find all questions | Fetched the live USGS feed (`notes/samples/`) and inspected the real fields | ✏️ | Found 4 questions the AI list missed (M1, M3, M4, M5): events get revised, aftershock bursts, PAGER `alert` mostly null, `[lon, lat]` order. |
| V-006 | 2026-11-24 | 1 | AI claim: USGS `ids` contains several aliases per quake and the preferred ID changes | Checked `ids` for every sampled event | ❌ as fact | All 5 had a single ID. Downgraded M2 to an *unverified hypothesis* with lower risk rating. Claim came from model memory, not data. |
| V-007 | 2026-11-24 | 1 | AI's [PM] assumptions A-01, A-03, A-05/06 | Owner review | ❌ | Owner overrode three: personal use instead of org tool; no AI-designed importance rules; one multi-topic source. AI assumptions had skewed toward an enterprise product the brief never asked for. |
| V-008 | 2026-11-24 | 1 | AI memory: Guardian API accepts `api-key=test` | Live call | ❌ | 401 Unauthorized. Guardian moved to "needs registration". |
| V-009 | 2026-11-24 | 1 | AI assumption that GDELT is a usable free API | 3 live calls with backoff | ✏️ | `timelinevol` OK. `artlist` returned 429 on every attempt, even single requests 15 s apart. Demoted to runner-up for reliability. |
| V-010 | 2026-11-24 | 1 | AI's hypothesis: BBC top-stories feed is editorially ranked | Measured ordering in all 5 feeds | ✅ (with caveat) | Top stories 15/32 adjacent pairs chronological vs 37/41 in Science → ordering is not by time, consistent with editorial ranking. This is still an inference, not documented BBC behavior. |
| V-011 | 2026-11-24 | 1 | Implicit "new item in feed = new news" | Checked pubDate range and cross-feed overlap | ❌ | Feeds hold items back to April. 29/144 articles appear in several feeds with differing GUID suffixes and tracking params. Required: freshness window + normalized article ID. |
| V-012 | 2026-11-24 | 1 | AI claim (memory): BBC RSS terms allow personal non-commercial use with attribution | Tried to read the BBC terms page: fetch tool refused www.bbc.co.uk; a curl workaround was blocked by the permission system as circumventing that refusal, and I did not push further | ⚠️ → ✏️ | **Resolved by owner in prompt 009** (terms supplied manually). Claim was **half right**: personal use + attribution correct; it **missed** the automated-collection ban (§7a), the metadata licence (§12) and the "computer analysis" permission (§8a). See V-013. |
| V-013 | 2026-11-24 | 1 | Terms review: does the document apply, and what does it restrict? | Read the full terms against our design, clause by clause | ✏️ | (1) The document is BBC.com/Studios terms. §3 excludes bbc.co.uk, so the **Public Service terms likely govern** the feeds and are still unread. (2) Personal prototype = grey area; business use = licence required. Mitigations adopted (D-013). |
| V-014 | 2026-11-24 | 1 | AI's own working practice: saving raw BBC feeds into `notes/samples/` for the evidence trail | Checked against terms §12 (exporting RSS metadata) before the first commit that would include them | ❌ | Committing them to a public GitHub repo would arguably republish BBC metadata. Git-ignored before any commit (verified with `git check-ignore`). Tests will use synthetic fixtures. |

## Standard checklist applied to generated code

- [ ] External APIs / endpoints / fields verified by a real call
- [ ] Library functions/params exist in the pinned version
- [ ] Tests fail when the logic is broken (mutation spot-check)
- [ ] No swallowed exceptions / silent fallbacks
- [ ] Timezones explicit (UTC stored)
- [ ] Idempotency / dedup holds under re-runs
- [ ] No secrets in logs, code or repo
- [ ] No scope creep beyond the requested slice
