# Prompt 009: BBC terms of use supplied by owner

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 1

## Prompt (verbatim)

> @"C:\Users\sipos\Downloads\BBC Global Terms of Use.txt"
> Here is the terms of use.

The attached file is the BBC Global Terms of Use (updated 15 April 2026). It
is **not** reproduced in this repo because it is BBC copyright.

## How the AI approached it

1. Checked **whether the document applies at all**. §3 excludes bbc.co.uk,
   so the Public Service terms likely govern the feeds.
2. Mapped each relevant clause (§1, 5b, 7a, 7c, 8a, 12) onto our design.
3. Checked its own working files against the terms. It found that the raw
   BBC feed captures it had saved for evidence would have been published to
   GitHub on the next commit, and git-ignored them first (V-014).
4. Proposed a compliance posture with mitigations (D-013) for the owner to
   confirm.

## Outcome

- `notes/bbc-terms-review.md`
- D-013 (pending confirmation)
- V-012 resolved; V-013 and V-014 added
- `.gitignore` updated
- `notes/samples/README.md` added
