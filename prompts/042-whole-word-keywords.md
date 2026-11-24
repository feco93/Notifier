# Prompt 042: Whole-word keyword matching

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-25 · **Phase:** 4

## Prompt (verbatim)

> Whole word or phrase

## How the AI approached it

1. Added 11 test cases first, including punctuation, possessives, `U.S.`,
   `C++` and multi-space phrases. 5 failed (red).
2. Implemented a regex with word boundaries, escaped words and `\s+` between
   phrase words, plus a timeout.
3. Mutation-tested the new rule. **One mutant survived**, exposing a missing
   test (a word suffix). Added the test; 6/6 killed.
4. Updated requirements US-05, design §4 row 6 and A-09.

## Outcome

- 97 tests green
- D-026, V-052
