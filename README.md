# Small Models, Sharp Jobs: Build a Local Agent That Proves Its Work

Attendees complete an evidence lookup tool, connect a typed extraction step, add one deterministic
verification rule, then run the application to create a claim ledger, verification report and cited
incident brief.

Workshop for Global AI Construct Brisbane, 31 August 2026.

## The thesis

Small models can do real work when they receive narrow jobs, useful tools, deterministic workflows
and verification.

The application reads a fictional service-incident evidence pack and produces three artifacts:

| Artifact | Who wrote what | What it is |
| --- | --- | --- |
| `artifacts/claim-ledger.json` | the model proposes each claim's kind, value and quote; code attaches the source id, normalizes kinds, merges duplicates, assigns claim ids and serializes the file | every claim, with the source and the exact quote behind it |
| `artifacts/verification.json` | ordinary code | `PASS` / `FAIL` / `UNVERIFIED` per claim per rule |
| `artifacts/incident-brief.md` | ordinary code | the brief, built only from claims that passed |

```text
evidence tool -> small-model typed extraction -> typed claim ledger
             -> deterministic verification -> deterministic Markdown renderer
```

Step order is a **deterministic C# pipeline around Microsoft Agent Framework agents and tools** —
plain code you can step through in a debugger. It does not use the MAF Workflows API.

**The model never writes the authoritative brief.** It proposes typed claims; code decides what is
true enough to print. "Proves its work" means declared checks over evidence IDs, quotes, dates,
numbers, event text and deterministic invariants. It does not mean proof of every semantic claim or
real-world truth.

## Run it

```bash
dotnet test                                                # 139 deterministic tests, no model needed
dotnet run --project src/Workshop.App -- ready             # can this machine do the workshop?
dotnet run --project src/Workshop.App -- run               # the full path
dotnet run --project src/Workshop.App -- gates --repeat 5  # the local model gate matrix
```

Defaults target Ollama on `http://localhost:11434/v1` with `nemotron-3-nano:4b`. The lane label
(`LOCAL` or `HOSTED`) is derived from the endpoint, not declared. See
[workshop/SETUP.md](workshop/SETUP.md) for prerequisites and the prefetch checklist.

## Watch verification do its job

```bash
dotnet run --project src/Workshop.App -- verify-only --inject-defect altered-number
```

Exit code 2, the number is excluded from the brief, and `verification.json` names the rule that
caught it. Each defect writes its own self-consistent trio into `artifacts/break-it/<defect>/`, so
the clean artifacts are never overwritten.

Six seeded defects: `phantom-source`, `altered-number`, `altered-timestamp`, `mislabelled-cause`,
`unsupported-event`, `spliced-quote`. Run them all with `scripts/demo-break-it.sh`.

## The rules

| Rule | What it checks |
| --- | --- |
| `R1-SOURCE-WHITELIST` | the cited source is one the tool is allowed to serve |
| `R2-QUOTE-PRESENT` | the quote occurs inside **one line** of the cited source |
| `R3` … `R7` | incident id, severity, customer count, timestamp and duration against facts parsed from source |
| `R8-REQUIRED-CLAIMS` | a brief is useless without incident id, severity and impact |
| `R9-CAUSE-UNVERIFIED` | anything asserting *why* is reported, never asserted |
| `R10-KNOWN-KIND` | the kind is one of the seven allowed |
| `R11-EVENT-SUPPORTED` | an event claim matches an event code parsed from `events.csv` |
| `R12-KIND-SEMANTICS` | a claim asserting causation must be labelled `cause`, whatever the model called it |

R9 and R12 work together: causal wording is detected from the claim text and its quote, so
relabelling a cause as an event does not get it past R9. All of it is token comparison against a
fixed marker list and against phrases parsed from the evidence. **There is no LLM judge anywhere.**

## Layout

```text
src/Workshop.Core   deterministic half: evidence store, parser, semantics, verifier, renderer  (no model, no MAF)
src/Workshop.App    the model half: Microsoft Agent Framework wiring and the CLI
tests/              139 deterministic tests + opt-in local-model tests
evidence-pack/      the fictional incident, plus a hidden answer key the model cannot read
starter/            attendee state, generated from src/ - four TODOs, compiles as-is
solution/           finished state, generated from src/
workshop/           agendas, runbook, setup, recovery card, demos, claims and limits, review lanes
workshop/reference-run/  the measured artifacts and gate report this repo's numbers come from
scripts/            starter generation, gates, offline proof, distribution checks, rehearsal timing
```

`starter/` and `solution/` are **generated** from `src/`, never hand-edited:

```bash
python3 scripts/generate-starter.py          # regenerate both
python3 scripts/generate-starter.py --check  # fail on drift
```

## Verify the whole thing

```bash
scripts/verify-all.sh              # everything: build, tests, drift, distribution, demos, gates
scripts/verify-all.sh              # SKIP_MODEL=1 to skip the lanes that need a runtime
scripts/reset-workshop.sh          # put the tree back between runs
```

## Status

Local core is verified on the reference machine; other lanes are not. **A non-author human has not
yet rehearsed the 60-minute path**, so the one-hour agenda is credible, not proven. Every claim, and
every lane that has *not* been run, is itemised in
[workshop/CLAIMS-AND-LIMITS.md](workshop/CLAIMS-AND-LIMITS.md). Read that before repeating any
number from this repo.
