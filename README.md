# Notifier — World-Event Alerts

Home assignment: take a deliberately vague product brief to a working
implementation using AI agents, with the process documented.

**Status:** Phase 4 (build), slices 1–2 of 7 done: solution, data model,
login; BBC source assembly.
Source: BBC News RSS, personal prototype (see
[notes/bbc-terms-review.md](notes/bbc-terms-review.md)).

## Run it (current state)

Requires the .NET SDK (10 or later; the app targets `net10.0` LTS).

```powershell
dotnet run --project src/Notifier.Web
```

Then open the URL it prints and log in. `run.ps1`, which also starts the
smtp4dev mail catcher, arrives with slice 4 (design §9a).

**Dev-only accounts** (hard-coded per A-01; hashes in
`src/Notifier.Web/appsettings.Development.json`, never use in production):

| User | Password | Role |
|---|---|---|
| alice | `alice-dev-only` | User |
| bob | `bob-dev-only` | User |
| admin | `admin-dev-only` | Admin |

To add or change a user, generate a hash with:

```powershell
dotnet run --project src/Notifier.Web -- hash-password
```

**Tests:**

```powershell
dotnet test --solution Notifier.slnx
```

**Database:** SQLite at `src/Notifier.Web/notifier.db`, created on first
start with `EnsureCreated()`. That call **never updates an existing schema**,
so delete the file after pulling a slice that changes the model (prototype
trade-off, design §3).

**Known prototype gaps** (deliberate, not production-ready):
- no login rate limiting or lockout
- no HTTPS redirect / HSTS
- no custom error page outside Development

## Where to look

| What | Where |
|---|---|
| Original brief | [docs/00-brief.md](docs/00-brief.md) |
| Plan of attack | [docs/plan.md](docs/plan.md) |
| Ambiguity analysis & assumptions | [docs/01-ambiguity-analysis.md](docs/01-ambiguity-analysis.md) |
| Requirements & scope | [docs/02-requirements.md](docs/02-requirements.md) |
| Design | [docs/03-design.md](docs/03-design.md) |
| Decisions | [docs/decision-log.md](docs/decision-log.md), [docs/adr/](docs/adr/) |
| Tech stack | [docs/adr/0001-tech-stack.md](docs/adr/0001-tech-stack.md) |
| How AI output was checked | [docs/validation-log.md](docs/validation-log.md) |
| Prompt history | [prompts/](prompts/) |
| Scratch / evidence | [notes/](notes/) |
