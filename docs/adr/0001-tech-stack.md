# 0001: Technology stack

Status: **accepted** (owner, prompt 017)
Date: 2026-11-24

## Context

- **Timing:** the stack was deliberately not chosen until requirements existed
  (D-004). It is decided here, before the detailed design, because the owner
  stated preferences and the design's shape (pages, hosting, auth) depends on
  it.
- **Owner preferences:**
  - Languages: C#, TypeScript
  - Frameworks: ASP.NET (LTS), React
- **Requirements that drive the choice** (`02-requirements.md`):

| Requirement | Implication |
|---|---|
| NFR-10 one command to run locally | Single deployable is strongly preferred |
| NFR-11 single process, handful of users | No distributed parts, no message broker |
| NFR-01 / AS-06 periodic + on-demand polling | In-process background worker with a manual trigger |
| NFR-04 once-only delivery | DB unique constraint on (article, alert, destination) |
| US-01 / AS-01 / NFR-12 hard-coded users, two roles | Simple cookie auth with a role claim |
| US-02 forms with validation; US-03 list/enable/delete | Form-heavy CRUD UI, ~8 screens |
| NFR-02 channels pluggable | Interface + DI registration |
| NFR-09 tests with synthetic fixtures, controllable time | Test framework + injectable clock |
| Budget: 3.5h build, Must scope ≈ 3–3.25h | Every extra moving part is expensive |

**Environment (verified, not assumed):**
- .NET SDK 11.0.100 installed
- ASP.NET Core runtimes 10.0.13 / 10.0.15 / 11.0.0
- Node 22.23.5

## Options considered

| Option | Pros | Cons |
|---|---|---|
| A. ASP.NET Core API + React SPA (Vite, TS) | Uses all four preferences | Two build pipelines, dev proxy/CORS, auth across SPA↔API, typed client. Estimated **+30–45 min**, which eats all build slack |
| **B. ASP.NET Core Razor Pages** | One project, one language, one process. Built-in cookie auth, folder-level role authorization, antiforgery on by default, model validation. Fits form-heavy CRUD | Drops TypeScript/React from the preference list |
| C. Razor Pages + htmx | B plus partial page updates | Extra dependency. Only worth it for 1–2 buttons |
| D. Blazor (static SSR) | Component model | More setup, form-handling quirks in static mode: medium risk |
| E. Next.js / React Router SSR | Keeps React | A second backend next to ASP.NET. Same two-stack cost as A |

## Decision

**B: ASP.NET Core Razor Pages on .NET 10 (LTS).** htmx is not adopted up
front; it may be added later for single buttons only if needed.

| Concern | Choice | Status |
|---|---|---|
| Runtime / target | `net10.0` (LTS), built with the installed .NET 11 SDK; ASP.NET Core 10 runtime present | Owner-accepted |
| UI | Razor Pages, server-rendered, minimal CSS | Owner-accepted |
| Local email catcher | **smtp4dev** (`rnwood.smtp4dev` 3.15.0). Spike: runs on the installed net10.0 runtime, receives MailKit mail | Owner-accepted, ✅ **verified** |
| Persistence | SQLite + EF Core; unique index for idempotency | AI default, not objected |
| Auth | Cookie authentication; users + role in config; dev-only passwords outside the repo | AI default, not objected |
| Background work | `BackgroundService` + `PeriodicTimer`; manual trigger via in-process channel | AI default, not objected |
| Clock | `TimeProvider` (built-in) injected everywhere time matters | AI default, not objected |
| RSS parsing | `System.ServiceModel.Syndication` 10.0.15 + **lenient `Rss20FeedFormatter.DateTimeParser`** (default parser fails the whole feed on one bad date) | ✅ **verified** by spike (prompt 018) |
| Email sending | MailKit 5.0.0 (MIT). Microsoft's docs recommend against `SmtpClient` for new code and name MailKit. Note the **5.x API** (`SendRequest`) | ✅ **verified** by spike + docs (prompt 018) |
| Slack | `HttpClient` (via `IHttpClientFactory`) POST to webhook; host allow-list `hooks.slack.com` | AI default |
| Tests | xUnit v3 (4.0.3, Apache-2.0) with **plain xUnit `Assert`**, no assertion library. FluentAssertions 8.x excluded: Xceed non-commercial licence, ✅ verified from the package | Owner-accepted (prompt 019) |
| Solution layout | `Notifier.Web` (Razor Pages host + worker), `Notifier.Core` (domain, matcher, channel interface, no framework refs), `Notifier.Tests` | AI default |

## Consequences

- **+** Fits the budget with roughly 0.25–0.5h slack restored. One `dotnet run`
  (plus smtp4dev) satisfies NFR-10.
- **+** Security defaults (antiforgery, auth cookies, role-protected `/Admin`
  folder) come from the framework instead of being hand-built.
- **−** TypeScript/React aren't demonstrated. If a richer UI is needed later,
  the Core library (matcher, channels, domain) is UI-independent and can sit
  behind an API.
- ~~**−** smtp4dev's target runtime is unverified.~~ Resolved: it targets
  net10.0 and runs (prompt 018). The Mailpit fallback is not needed.
- **Design constraints discovered by the spikes:**
  - treat a missing or unparseable `pubDate` as "unknown" (not eligible)
  - store `published_at` as UTC
  - article ID = last URL path segment across sections
  - strip CR/LF from titles before they go into email subjects
  
  Details: [notes/spikes/README.md](../../notes/spikes/README.md).

## How the AI was involved / what was challenged

- The AI first proposed a stack (Python/FastAPI) **before** requirements
  existed. The owner rejected that ordering (D-004, V-002).
- After the owner stated preferences, the AI recommended React and then
  flagged its own cost estimate. The owner chose to cut cost.
- The AI's smtp4dev-vs-Mailpit comparison was **unfair**: it framed Mailpit as
  Docker-only, but Mailpit also ships as a standalone binary. Corrected when
  the owner asked why (V-018).
- Several defaults rest on model memory and are explicitly marked **to
  verify** in phase 4 rather than accepted as fact: the FluentAssertions
  licence, Microsoft's SmtpClient guidance (MailKit), Syndication handling BBC
  feeds, and smtp4dev's runtime.
