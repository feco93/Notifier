# Prompt 008: Confirm source: BBC News RSS

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 1

## Prompt (verbatim)

> BBC News RSS

## Sub-prompt to a web-fetch subagent (verbatim)

> Fetch https://www.bbc.co.uk/news/10628494 (BBC "News feeds from the BBC"
> page about RSS). If it doesn't contain usage terms, also try
> https://www.bbc.co.uk/usingthebbc/terms/ . Answer concisely: (1) Under what
> terms may BBC News RSS feeds be used — is personal, non-commercial use
> allowed? (2) Any attribution / linking-back requirements? (3) Any
> restrictions on automated polling frequency or on reproducing full article
> text? (4) Does the page list available topic feeds (e.g. world, business,
> science_and_environment, technology) — list the feed URLs it gives. Quote
> short phrases (under 15 words) as evidence and give the exact URL each claim
> came from. If the pages don't address a point, say so explicitly rather
> than guessing.

**Subagent result:** the fetch layer refused www.bbc.co.uk. The subagent
correctly reported "unknown" instead of filling in from memory.

## Outcome

- A-02′…A-04′ finalized: importance = BBC editorial prominence, with
  Headline / Front page / All tiers, a freshness window, and normalized
  article identity.
- D-011 accepted.
- The terms of use remain unverified (V-012).
