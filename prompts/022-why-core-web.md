# Prompt 022: Why Notifier.Core and Notifier.Web?

**Tool:** Claude Code (Opus 5.5) · **Date:** 2026-11-24 · **Phase:** 3

## Prompt (verbatim)

> Why did you propose the Notifier.Core and Notifier.Web assemblies? What was the rationale behind them?

## How the AI approached it

1. Gave the four intended reasons:
   - compiler-enforced boundary
   - testability
   - source isolation (NFR-03)
   - channel contract (NFR-02)
2. While justifying reason 3, found that its own design **didn't deliver
   it**: BBC parsing sat in Core and no source interface existed.
3. Admitted that its earlier trace script had reported NFR-03 as covered only
   because the ID was mentioned (V-026).
4. Offered three options with trade-offs: (A) 2 projects + interface,
   (B) 1 project, (C) 3 projects.

## Outcome

Owner chose A (prompt 023).
