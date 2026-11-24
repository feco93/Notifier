# Prompt History

Every prompt given to an AI agent during this project, verbatim, in order.
Each file: the prompt, the tool/model used, and a short outcome note linking
to the validation log where relevant.

Tool: Claude Code (desktop app), model Claude Opus 5.5, unless noted otherwise.

Full session transcripts are exported to `prompts/transcripts/` at milestones.

| # | File | Purpose |
|---|---|---|
| 001 | [001-high-level-plan.md](001-high-level-plan.md) | Ask for a high-level plan from the assignment |
| 002 | [002-scaffold-repo.md](002-scaffold-repo.md) | Start phase 0 in the chosen folder |
| 003 | [003-defer-stack.md](003-defer-stack.md) | Challenge: defer stack decision until requirements exist |
| 004 | [004-rescope-6-8h.md](004-rescope-6-8h.md) | Re-scope plan to a 6–8h budget |
| 005 | [005-commit-phase0.md](005-commit-phase0.md) | Commit phase 0 |
| 006 | [006-start-phase1.md](006-start-phase1.md) | Phase 1: ambiguity analysis |
| 007 | [007-owner-review-assumptions.md](007-owner-review-assumptions.md) | Owner overrides assumptions; source evaluation |
| 008 | [008-confirm-bbc.md](008-confirm-bbc.md) | Source confirmed (BBC RSS); importance assumptions finalized |
| 009 | [009-bbc-terms.md](009-bbc-terms.md) | BBC terms reviewed; compliance posture; raw feed data kept out of repo |
| 010 | [010-keep-bbc.md](010-keep-bbc.md) | Owner accepts BBC with mitigations; phase 1 closed |
| 011 | [011-commit-phase1.md](011-commit-phase1.md) | Commit phase 1 |
| 012 | [012-start-phase2.md](012-start-phase2.md) | Phase 2: requirements + traceability check |
| 013 | [013-reprioritize.md](013-reprioritize.md) | Owner re-prioritizes AS-03/04/05, US-04 |
| 014 | [014-commit-phase2.md](014-commit-phase2.md) | Commit phase 2 |
| 015 | [015-stack-preferences.md](015-stack-preferences.md) | Owner's stack preferences; remaining decisions |
| 016 | [016-server-rendered-options.md](016-server-rendered-options.md) | Server-rendered options; smtp4dev rationale |
| 017 | [017-choose-razor-smtp4dev.md](017-choose-razor-smtp4dev.md) | Stack accepted: Razor Pages + smtp4dev (ADR 0001) |
| 018 | [018-verify-libraries.md](018-verify-libraries.md) | Spikes: RSS parsing, email, test library licences |
| 019 | [019-plain-assert.md](019-plain-assert.md) | Assertion style: plain xUnit Assert |
| 020 | [020-commit-stack.md](020-commit-stack.md) | Commit stack ADR + spikes |
| 021 | [021-start-design.md](021-start-design.md) | Phase 3 design + ADRs 0002/0003, self-checks |
| 022 | [022-why-core-web.md](022-why-core-web.md) | Why two projects? Exposed NFR-03 not actually met |
| 023 | [023-option-a.md](023-option-a.md) | Two projects + IArticleSource |
| 024 | [024-separate-bbc-assembly.md](024-separate-bbc-assembly.md) | Owner asks: separate BBC assembly? |
| 025 | [025-update-design-bbc-assembly.md](025-update-design-bbc-assembly.md) | Notifier.Sources.Bbc adopted |
| 026 | [026-explain-questions.md](026-explain-questions.md) | In-depth Q1–Q5; data refutes Q1 default |
| 027 | [027-decide-q1-q5.md](027-decide-q1-q5.md) | Owner decides Q1–Q4, rejects Q5; asks about batching + retention |
| 028 | [028-adopt-batching-retention.md](028-adopt-batching-retention.md) | Batching + retention adopted; build re-budgeted |
| 029 | [029-commit-design.md](029-commit-design.md) | Commit phase 3 |
| 030 | [030-start-slice1.md](030-start-slice1.md) | Slice 1: skeleton, data model, login; 14 tests |
| 031 | [031-review-slice1.md](031-review-slice1.md) | Code review: logout bug found by a new failing test; DB path fix |
| 032 | [032-no-chatty-comments.md](032-no-chatty-comments.md) | Comment convention: short whys only, no process IDs in code |
| 033 | [033-commit-slice1.md](033-commit-slice1.md) | Commit slice 1; SQLite side files slipped in, fixed |
| 034 | [034-start-slice2.md](034-start-slice2.md) | Slice 2: BBC source assembly; live check; 46 tests |
| 035 | [035-mask-username.md](035-mask-username.md) | Mask local username in verbatim prompts |
| 036 | [036-keep-history.md](036-keep-history.md) | Owner keeps git history unchanged |
| 037 | [037-review-slice2.md](037-review-slice2.md) | Review slice 2: unsafe URL schemes and unvalidated config fixed test-first |
| 038 | [038-why-duplicates.md](038-why-duplicates.md) | Owner challenge: suffix is a section index, not a repeat counter |
| 039 | [039-lead-section-only.md](039-lead-section-only.md) | Headline ranks only the lead section (D-025) |
| 040 | [040-commit-slice2.md](040-commit-slice2.md) | Commit slice 2 |
| 041 | [041-start-slice3.md](041-start-slice3.md) | Slice 3: matcher test-first; 11/11 mutants killed; keyword noise found |
| 042 | [042-whole-word-keywords.md](042-whole-word-keywords.md) | Keywords as whole word/phrase; surviving mutant exposed a missing test |
| 043 | [043-review-slice3.md](043-review-slice3.md) | Review slice 3: UTC guard; own hypothesis refuted by test; 17/17 mutants |
| 044 | [044-commit-slice3.md](044-commit-slice3.md) | Commit slice 3 |
