# Prompt 016: Server-rendered options; why smtp4dev?

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 3

## Prompt (verbatim)

> I take your advice to reduce cost. What are the options for a server-rendered page app?
> Why did you choose smtp4dev as a local email catcher?

## How the AI approached it

1. Compared five server-rendered options against the requirements and
   budget:
   - Razor Pages
   - Razor Pages + htmx
   - MVC
   - Blazor static SSR
   - Next.js / React Router SSR

   It noted that the TypeScript SSR options don't actually save cost.
2. Verified smtp4dev exists on NuGet with a live `dotnet tool search` before
   defending the choice.
3. Admitted the earlier comparison framed Mailpit unfairly as Docker-only
   (V-018). Flagged smtp4dev's target runtime as unverified.

## Outcome

Recommendation: Razor Pages; smtp4dev with Mailpit as fallback.
