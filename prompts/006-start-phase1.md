# Prompt 006: Start phase 1 (ambiguity analysis)

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 1

## Prompt (verbatim)

> Start phase 1

## How the AI approached it

1. Generated an unfiltered list of 38 PM questions from the brief alone. Kept
   verbatim as an intermediate output in `notes/phase1-raw-questions.md`.
2. Fetched the live USGS feed to check the brief-only list against real
   data. The sample is saved in `notes/samples/`.
3. Triaged each question, added the questions only the real data revealed,
   and wrote assumptions A-01…A-18.
4. Checked its own claim about USGS ID aliasing against the sample, found it
   wasn't supported, and downgraded it.

## Outcome

`docs/01-ambiguity-analysis.md`, decisions D-006…D-008, validation entries
V-004…V-006.

## Reviewer caveat

The same model generated and critiqued the question list, so the critique is
only partly independent. The independent checks are the live-data
comparison and **my review of the [PM]-marked assumptions** (A-01, A-05,
A-11).
