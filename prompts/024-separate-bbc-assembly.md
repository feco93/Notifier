# Prompt 024: Separate assembly for the BBC source?

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 3

## Prompt (verbatim)

> Have you considered moving Bbc source into a separate assembly?

## How the AI approached it

1. Admitted it hadn't considered this narrow variant. It had only offered a
   broad Infrastructure layer (option C), which it rejected as ceremony
   (V-028).
2. Compared it with option A on encapsulation (`internal` + one public
   registration method), NFR-03 concreteness, dependency isolation and
   testability.
3. Was explicit about the limits:
   - Web still references the assembly to register it.
   - The same argument applies to channels. It recommended keeping those in
     Web, with the reason recorded.

## Outcome

Recommendation: adopt `Notifier.Sources.Bbc`. The owner agreed (prompt 025).
