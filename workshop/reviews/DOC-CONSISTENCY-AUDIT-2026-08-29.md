# Documentation consistency audit — 29 August 2026

Scope: every Markdown file under `workshop/` (including `workshop/data/`, `workshop/reference-run/`,
`workshop/reviews/`), plus `README.md`, `starter/README.md`, `solution/README.md`, `AGENTS.md`,
`CLAUDE.md`, and the root `evidence-pack/` directory. Checked against `src/Workshop.App/Program.cs`
(commands: `gather`, `run`, `workflow`, `ready` — no `gates`, `verify-only`, `smoke` case in the
switch), `src/Workshop.Core/CrashWorkflow.cs` (gate names), a live `dotnet test` run (129 passed in
`Workshop.Core.Tests`; 22 passed / 4 skipped in `Workshop.LocalModel.Tests`), and the authoritative
docs named in the task.

**Note on repo state:** this is a git repo with substantial uncommitted changes already on disk
(`git status` shows most of the suspect list already modified, and the old `evidence-pack/`-based
`solution/`/`starter/` files already deleted). Several files below were already migrated to the new
scenario before this audit started — verdicts reflect current disk content, not git history.

## Verdict table

| File | Verdict | Action taken |
| --- | --- | --- |
| `README.md` | current | none needed |
| `AGENTS.md` | current | none needed |
| `CLAUDE.md` | current | none needed |
| `starter/README.md` | current | none needed |
| `solution/README.md` | current | none needed |
| `workshop/WORKSHOP-SPEC-2026-08-29.md` | current | none needed (authoritative) |
| `workshop/CHECKPOINTS.md` | current | none needed |
| `workshop/AGENDA-90.md` | current | none needed — no stray "90 minutes"; segments sum to 85 |
| `workshop/data/README.md` | **mixed** | not fixed — see note below (out of explicit fix list) |
| `workshop/data/SOURCE-ASSESSMENT.md` | current | none needed |
| `workshop/ATTENDEE-GUIDE.md` | current | none needed |
| `workshop/HYBRID-DELIVERY-CARD.md` | current | none needed |
| `workshop/ADVANCED-OPENAI-COMPATIBLE-RECOVERY.md` | current | none needed |
| `workshop/ORGANISER.md` | current | none needed |
| `workshop/FACILITATOR-RUNBOOK.md` | current | none needed |
| `workshop/DEMOS.md` | current | none needed |
| `workshop/RECOVERY-CARD.md` | current | none needed |
| `workshop/REHEARSAL.md` | current | none needed |
| `workshop/reference-run/README.md` | current | none needed |
| `workshop/SETUP.md` | was stale | **fixed** — see below |
| `workshop/GLOSSARY.md` | was stale | **fixed** — see below |
| `workshop/CLAIMS-AND-LIMITS.md` | stale (whole-file) | **not rewritten** — historical banner added instead; see rationale below |
| `workshop/LM-STUDIO-SWEEP.md` | stale (whole-file) | **historical banner added** (was missing one) |
| `workshop/AGENDA-60.md` | stale (whole-file) | already banner-marked by prior work; left as-is |
| `workshop/AGENDA-120.md` | stale (whole-file) | already banner-marked by prior work; left as-is |
| `workshop/reviews/*.md` (6 files) | out of scope | process/decision records from the pivot itself, not attendee/facilitator docs — not line-audited |
| root `evidence-pack/` | **delete-candidate** | left in place per instructions |

## Fixed: `workshop/SETUP.md`

- Line 3–5 (old): *"Attendees complete an evidence lookup tool, connect a typed extraction step, add
  one deterministic verification rule, then run the application to create a claim ledger,
  verification report and cited incident brief."* — old scenario (tool/claim-ledger/incident-brief).
  Replaced with a description of the actual two TODOs (Extract, Analyse in `CrashPipeline.cs`) behind
  Gather and the code-owned gates.
- Line 39 (old): `dotnet test # expect 142 passed` — stale count (142 was the old scenario's
  120-core + 22-provider-seam total). Corrected to 129, matching the live `Workshop.Core.Tests` run.
- Line 40/43 (old): `# expect: READY: PASS` / *"If step 4 prints `READY: PASS`..."* — `Program.cs`
  actually prints `"READY: model-backed supported path completed."` (`Program.cs:62`); `READY: PASS`
  never existed for this command. Corrected both occurrences, and added `--term intersection` to the
  example command since `ready` needs a matching Gather result (`Program.cs:60`).
- Line 47 (old): *"A machine can answer `JACKDAW_OK` and still fail the workshop."* — `JACKDAW_OK` is
  the literal token from the old `SmokeAsync()` method in the now-unused `IncidentPipeline.cs`; there
  is no `smoke` command wired into the current `Program.cs` switch. Replaced with a sentence about the
  actual `ready` gate.

## Fixed: `workshop/GLOSSARY.md`

- **Tool** entry (old lines 16–18) described `read_evidence`, a model-callable whitelisted file
  reader — the current design deliberately has no tool call at all (Gather runs as plain C# before
  the model is invoked). Rewritten.
- **Workflow** entry (old lines 20–22): *"fetch evidence, extract claims, verify, render"* — rewritten
  to Gather / Extract / Analyse plus gate, matching `CrashPipeline.cs`.
- **Claim ledger** entry (old lines 24–26, `claim-ledger.json`) — no such file/concept exists now.
  Replaced with a **Selection** entry describing `CrashSelection`.
- **Verifier** entry (old lines 28–37, `verification.json`, PASS/FAIL/UNVERIFIED per rule) — replaced
  with a **Gate** entry naming the real `CrashGate` values (`Supported`, `NoEvidence`,
  `UnsupportedSelection`, `LowConfidence`, `UnsupportedAnalysis`) from `CrashWorkflow.cs`.
- **Renderer** entry (old lines 39–40, wrote `incident-brief.md`) — removed; there is no renderer step
  in the new pipeline (the CLI prints gate/JSON directly).
- **Ground truth / source parsing** entry (old lines 42–44, regex/CSV cross-check against the model) —
  removed; no such independent cross-check exists in `CrashWorkflow.cs`.
- **Seeded defect** entry (old lines 46–49, six defects / `--inject-defect`) — removed; no such flag
  exists in the current `Program.cs`.
- **Causal marker** entry (old lines 59–62, `R12-KIND-SEMANTICS`) — removed; no rule-ID system exists
  in the new pipeline.
- **Typed extraction** entry also referenced *"a list of claims, each with a kind, a value and a
  quote"* and *"the verifier's job"* — updated to match Extract's actual shape (record IDs, rationale,
  confidence) and the renamed **Gate** entry.

`LOCAL / FREE CLOUD / CONTROLLED CLOUD` was left untouched — it's a generic concept, still accurate.

## Follow-up flag: `workshop/SETUP.md` still links to the now-historical `CLAIMS-AND-LIMITS.md`

Lines 58 and 68 (post-fix) point Native Windows and Apple Silicon readers at
`[CLAIMS-AND-LIMITS.md](CLAIMS-AND-LIMITS.md)` for "not run end-to-end by us" platform caveats. That
file is now banner-marked historical (see below) rather than rewritten for the new scenario, so the
link still resolves but its content is about the old pipeline. Not changed here — redirecting it
needs either a new claims-and-limits doc for the new scenario or a decision to drop the caveat, both
beyond a docs-consistency edit.

## Not fixed, with reason: `workshop/CLAIMS-AND-LIMITS.md`

This file was on the "fix directly" list, but on inspection its entire body (measured latencies, test
counts, `R1`–`R12` rule IDs, `gates --repeat 5`, the LM Studio sweep, the 60-minute rehearsal script)
is a *measurement report* against the old evidence-pack pipeline, not a document with a few stale
terms in an otherwise-current structure. Examples of what would need to change: line 22 `per-call
request budget 90 s` (still true, but attached to an old-scenario provenance block); line 37 `142/142
... 120 core + 22 provider-seam`; line 38 `Workshop.App gates --repeat 5`; lines 52–53, 154, 159–168
naming `R1`–`R12` rules that no longer exist; line 46/47/50/100/106 naming the 60-minute path.

None of this is "short edit" territory — a correct replacement would require re-running the full
measurement suite (timing, GPU placement, LM Studio sweep, seeded-defect runs) against the new Gather
→ Extract → Analyse pipeline, which no longer has a `gates` command to run those measurements with.
Fabricating new numbers to make the edit "look" current would violate the file's own MEASURED /
DOCUMENTED / INFERRED / UNVERIFIED discipline. I added the same historical banner used on
`AGENDA-60.md` / `AGENDA-120.md` instead of rewriting it:

```
> Historical (pre-29-Aug pivot): describes the earlier incident-pack build. Current path: AGENDA-90.md.
```

If a claims-and-limits doc is wanted for the new scenario, that needs a fresh measurement pass, not a
doc edit — worth a separate task.

## Historical banner added: `workshop/LM-STUDIO-SWEEP.md`

Same rationale as above (whole-file measurement report against the old `gates` pipeline: `gates
--repeat 5`, six seeded defects, `claims/pass/fail/unver` counts). This one had **no** historical
marker at all before this pass, unlike `AGENDA-60/120`. Added the same banner line at the top; body
left untouched per instructions.

## Already handled: `workshop/AGENDA-60.md`, `workshop/AGENDA-120.md`

Both already carry a historical marker from prior work (not this pass):

```
# Archived 60-minute agenda — not the delivery path
> This historical fictional-evidence version is retained for reference only. Do not use it for the
> Victorian crash workshop; use `AGENDA-90.md`.
```

(and the `120`-minute equivalent). This satisfies the spirit of the requested banner — same warning,
same pointer to `AGENDA-90.md` — just worded slightly differently from the exact line specified in the
task. I left it as-is rather than adding a second, redundant banner. Bodies still contain old-scenario
content (`evidence-pack/`, `claim ledger`, `142 passed`, `R2-QUOTE-PRESENT`, `verify-only
--inject-defect`, `incident-brief.md`) — correctly untouched per "do NOT rewrite."

## Flagged, not fixed: `workshop/data/README.md`

This file is on the "authoritative" list, but it contradicts itself and the running code:

- Line ~56–58: *"`synthetic-incident-records.json` is fictional training data... **It remains the
  default dataset** and recovery fallback until an end-to-end rehearsal adopts the Victorian
  sample."*
- But `src/Workshop.App/Program.cs:6` hardcodes the CLI default to
  `"workshop/data/victoria-road-crash-sample.json"`, and `workshop/WORKSHOP-SPEC-2026-08-29.md:40`
  independently states *"The ready Victorian crash sample is the default workshop dataset."*

So the code and the spec agree the Victorian sample is already the default; this file's synthetic
section says the opposite. Left unedited since it wasn't in the explicit fix list and the team lead
may have context on which statement is intentional (e.g. a rehearsal-gate not yet cleared) — flagging
rather than guessing.

## Delete-candidate: root `evidence-pack/`

Still present (`customer-email.txt`, `events.csv`, `expected-facts.json`, `runbook.md`, `status.txt`)
even though `starter/evidence-pack/` and `solution/evidence-pack/` are already deleted (confirmed via
`git status`). Nothing current references it — only the already-historical `AGENDA-60.md:24` and
legacy scripts (`scripts/demo-clean-run.sh`, `scripts/rehearse-60.sh`, `scripts/reset-workshop.sh`).
Left in place; deleting it is a call for the team lead since scripts still exist that touch it.

## Out of scope: `workshop/reviews/*.md`

`BONSAI-27B-CHECKPOINT-2026-08-29.md`, `FABLE-PLAN-2026-08-29.md`, `FABLE-PLAN.md`,
`FABLE-VERIFICATION.md`, `OPUS-DISPOSITION.md`, `SOL-REVIEW.md` — these are dated records of the
pivot's own planning/review process, not attendee- or facilitator-facing workshop docs. Not in the
task's named suspect list; not line-audited here.
