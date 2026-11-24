# Prompt 038: Challenge: why duplicates, and is "first occurrence" right?

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 4

## Prompt (verbatim)

> I noticed the following in the BbcLiveSmoke:
>
> ```
> The GUID `#0/#1/#2` suffixes appear to number these repeats.
> - Keeping the first occurrence is correct, and for Top stories it is also
>   the best rank.
> ```
>
> Are you sure to keep the first occurrence? Have you figured out why there are duplicates?

## How the AI approached it

1. Admitted that both statements were untested inferences.
2. Analysed the captured feeds:
   - GUID suffix sequence along each feed
   - positions and suffixes of every duplicate
   - chronology inside each block
3. Found that the suffix is a **section index** (the feed concatenates page
   modules), refuting its claim. Found a missed Top stories duplicate.
   Re-confirmed V-010 at block level.
4. Concluded:
   - de-dup by first occurrence stays correct, for a corrected reason
   - Headline ranking has a gap when the lead block has < N items
5. Corrected the notes; put the ranking change to the owner.

## Outcome

V-048; ranking decision pending.
