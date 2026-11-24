# Prompt 046: Start slice 4 (channels, test send, run.ps1)

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-25 · **Phase:** 4

## Prompt (verbatim)

> Start slice 4

## Sub-prompt to a web-fetch subagent (verbatim)

> Answer three questions about Slack incoming webhooks and message formatting, using only Slack's official documentation. Start with these URLs (follow links on docs.slack.dev / api.slack.com if they redirect): https://api.slack.com/reference/surfaces/formatting , https://api.slack.com/messaging/webhooks , https://api.slack.com/methods/chat.postMessage .
>
> 1. Escaping: which characters must be escaped in message `text` (mrkdwn), and to what? Quote the exact guidance (under 25 words).
> 2. Links: what is the syntax for a link with custom text in mrkdwn, and is there guidance about characters inside the URL or label?
> 3. Length: what limits apply to the `text` field length (e.g., recommended max and truncation point)? Quote the numbers and where they come from.
> 4. Incoming webhook errors: what HTTP status codes and response bodies are documented for failed webhook posts (e.g., invalid_payload, no_service, channel_not_found)? List them.
>
> For each answer give the exact URL it came from. If a page doesn't say something, state that explicitly instead of filling it in from memory.

## How the AI approached it

1. Verified the Slack rules from the docs first. That changed the batch
   design (4,000-character budget).
2. Added the channel contract to Core.
3. Wrote tests first:
   - formatting
   - escaping
   - the SSRF guard
   - masking
   - sending
   - registry extensibility
   - test send
4. Implemented:
   - email (MailKit behind a transport interface)
   - Slack (no redirects, re-validated at send time)
   - registry and test-send service
   - `run.ps1` + pinned smtp4dev
5. Ran the real `run.ps1` and a real email end to end; verified clean
   shutdown.
6. 18 mutations: 3 survivors → 2 missing tests added, 1 dead code removed.

## Outcome

- 162 tests + 1 gated E2E
- V-055…V-058
