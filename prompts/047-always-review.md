# Prompt 047: Always review before finishing a slice

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-25 · **Phase:** 4

## Prompt (verbatim)

> Please always review your code first before finishing a slice.

## How the AI approached it

1. Made it a standing rule:
   - saved to the assistant's memory
   - recorded as D-027
   - added to the process in `plan.md`
2. Reviewed slice 4 immediately:
   - **`TestSendService`:** 3 findings → tests red → guards → mutation-killed
   - **`run.ps1`:** an ignored exit code, plus a silent failure when a port
     is taken
3. Tested the `run.ps1` fix against a real squatter process. That
   **refuted the AI's first fix** (smtp4dev keeps its web UI up without
   SMTP). Replaced it with an SMTP-greeting check and re-verified both
   scenarios.

## Outcome

- 165 tests + 1 gated E2E
- V-059, V-060, D-027
