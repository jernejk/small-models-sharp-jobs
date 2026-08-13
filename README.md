# Small Models, Sharp Jobs: Build a Local Agent That Proves Its Work

Attendees complete an evidence lookup tool, connect a typed extraction step, add one deterministic
verification rule, then run the application to create a claim ledger, verification report and cited
incident brief.

Workshop for Global AI Construct Brisbane, 31 August 2026.

## The thesis

Small models can do real work when they receive narrow jobs, useful tools, deterministic workflows
and verification.

The application reads a fictional service-incident evidence pack and produces three artifacts:

| Artifact | Written by | What it is |
| --- | --- | --- |
| `artifacts/claim-ledger.json` | small model, typed | every claim, with the source and the exact quote behind it |
| `artifacts/verification.json` | ordinary code | `PASS` / `FAIL` / `UNVERIFIED` per claim per rule |
| `artifacts/incident-brief.md` | ordinary code | the brief, built only from claims that passed |

```text
evidence tool -> small-model typed extraction -> typed claim ledger
             -> deterministic verification -> deterministic Markdown renderer
```

**The model never writes the authoritative brief.** It produces typed claims; code decides what is
true enough to print. "Proves its work" means declared checks over evidence IDs, quotes, dates,
numbers and deterministic invariants. It does not mean proof of every semantic claim or real-world
truth.

## Run it

```bash
dotnet test                                              # 63 deterministic tests, no model needed
dotnet run --project src/Workshop.App -- run             # the full path
dotnet run --project src/Workshop.App -- gates --repeat 5  # the local model gate matrix
```

Defaults target Ollama on `http://localhost:11434/v1` with `nemotron-3-nano:4b`. See
[workshop/SETUP.md](workshop/SETUP.md) for prerequisites and the prefetch checklist.

## Watch verification do its job

```bash
dotnet run --project src/Workshop.App -- verify-only --inject-defect altered-number
```

Exit code 2, the number is excluded from the brief, and `verification.json` names the rule that
caught it. Also try `phantom-source` and `altered-timestamp`.

## Layout

```text
src/Workshop.Core   deterministic half: evidence store, parser, verifier, renderer  (no model, no MAF)
src/Workshop.App    the model half: Microsoft Agent Framework wiring and the CLI
tests/              63 deterministic tests + opt-in local-model tests
evidence-pack/      the fictional incident, plus a hidden answer key the model cannot read
starter/            attendee state, generated from src/ - four TODOs, compiles as-is
solution/           finished state, generated from src/
workshop/           agendas, runbook, setup, recovery card, demos, claims and limits
scripts/            starter generation, gates, offline proof, rehearsal timing
```

`starter/` and `solution/` are **generated** from `src/`, never hand-edited:

```bash
python3 scripts/generate-starter.py          # regenerate both
python3 scripts/generate-starter.py --check  # fail on drift
```

## Status

Local core is verified on the reference machine; other lanes are not. Every claim, and every lane
that has *not* been run, is itemised in
[workshop/CLAIMS-AND-LIMITS.md](workshop/CLAIMS-AND-LIMITS.md). Read that before repeating any
number from this repo.
