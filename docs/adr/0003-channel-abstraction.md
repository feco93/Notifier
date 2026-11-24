# 0003: Channel abstraction: self-describing channels via DI

Status: **accepted**. Q2 (≤ 1 destination per channel per alert) accepted
by owner in prompt 027.
Date: 2026-11-24

## Context

The brief: email and Slack now, "flexible enough that we can add more channels
later". NFR-02 makes this testable: a new channel means one class plus a
registration, with **no changes** to matching, dispatch, the alert model or
pages.

## Options considered

| Option | Adding a channel requires | Verdict |
|---|---|---|
| A. Entity per channel type (TPH/TPT: `EmailDestination`, `SlackDestination`) + page per channel | New entity, schema change, new form | ❌ Violates NFR-02 |
| B. Runtime-loaded plugin assemblies | Nothing in the host, but needs plugin loading, versioning, isolation | ❌ Speculative; the brief only says "later" |
| **C. `INotificationChannel` with self-described config fields, registered in DI. Destination stores `ChannelKey` + JSON config** | One class + one DI line | ✅ |

## Decision

Option C. The interface (design §7) exposes:
- `Key` and `DisplayName`
- `ConfigFields`, each with `IsSecret`, used to render the form generically
- `Validate`
- `Describe`, which returns a masked description
- `SendAsync(Notification, config)`

The core builds a **channel-neutral `Notification`**. Each channel owns its own
formatting and escaping.

In v1, an alert has **at most one destination per channel**. This keeps the
create form a static list of channel blocks with no JavaScript (design §12
Q2).

## Consequences

- **+** NFR-02 holds and is **proven by a test** that registers a test-only
  channel and runs the dispatcher.
- **+** Secrets handling is centralized: `IsSecret` fields are masked
  everywhere and never logged.
- **−** Config is stored as JSON, so there's no DB-level validation of channel
  config. Validation lives in each channel's `Validate`.
- **−** "At most one destination per channel per alert" is a v1 limit. Lifting
  it is a UI change only; the model already supports N.

## How the AI was involved / what was challenged

- The AI's first channel sketch (phase 2 stack discussion) only made sending
  pluggable.
- During design, the AI noticed that a hand-written destination form would
  still need a page change per channel, which breaks NFR-02's "no page
  changes". That is why `ConfigFields` was added.
- Slack escaping rules and webhook error formats come from model memory.
  They are **to verify** in slice 4.
