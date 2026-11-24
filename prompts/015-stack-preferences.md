# Prompt 015: Owner's stack preferences; what else to decide?

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 3

## Prompt (verbatim)

> Before starting phase 3, I'd like to discuss my preferences for the selected programming language and frameworks.
> Programming language: C#, TypeScript
> Frameworks: ASP.NET (LTS version), React
>
> What else do we have to decide based on the requirements?

## How the AI approached it

1. Checked the local toolchain first (`dotnet --list-sdks`,
   `--list-runtimes`, `node -v`). Found SDK 11 only, plus ASP.NET Core 10
   runtimes, so `net10.0` LTS works without installs (V-019).
2. Listed the remaining decisions, each tied to a requirement ID:
   - 7 needing owner input (hosting, DB, auth, email catcher, UI kit, API
     style, tests)
   - defaults it would apply itself
3. Flagged the cost of API + React (~30–45 min) against the build budget.
4. Marked memory-based claims as unverified (FluentAssertions licence,
   SmtpClient guidance).

## Outcome

Discussion only; decisions recorded after prompt 017.
