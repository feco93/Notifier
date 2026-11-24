# Prompt 018: Verify RSS, email and test choices before design

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 3

## Prompt (verbatim)

> Before moving to phase 3, I'd like to decide the followings that are flagged to verify:
>
> * RSS parsing
> * Email sending
> * Tests

## Sub-prompt to a web-fetch subagent (verbatim)

> Fetch https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient
> and answer concisely: (1) Does Microsoft's documentation recommend against
> using System.Net.Mail.SmtpClient for new development? Quote the exact
> sentence (under 25 words) and say which section it is in. (2) Does it name
> an alternative library (e.g. MailKit)? (3) Is the class marked [Obsolete]
> in the API signature shown? If the page doesn't address a point, say so
> explicitly — do not answer from memory.

## How the AI approached it

1. Pulled licence and target framework metadata for 8 candidate packages
   straight from the NuGet API. Downloaded the FluentAssertions 8 package and
   read its LICENSE file.
2. **RSS spike** (`notes/spikes/rss-syndication`): parsed the real BBC
   captures and synthetic edge cases. Found that one malformed date kills the
   whole feed, and proved the fix.
3. **Email spike** (`notes/spikes/email-mailkit`): installed smtp4dev into a
   temporary folder (not globally), sent via MailKit, inspected the result
   through smtp4dev's API, and tested header injection.
4. Two AI-written API calls failed to compile because they came from older
   library versions. One of its own checks gave a false pass. All caught and
   corrected (V-022, V-023).

## Outcome

- `notes/spikes/README.md`
- ADR 0001 updated (✅ verified)
- D-018, V-020…V-024
- **Open:** owner to choose the assertion style.
