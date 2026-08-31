# Legacy-build cleanup — report

Working copy: `/tmp/cleanup-repo` (copy of `~/Developer/personal/pocs/global-ai-construct-offline-workshop`).
The original repo was not touched.

## Test counts

| Suite | Before | After |
|---|---|---|
| `Workshop.Core.Tests` | 129 passed | **9 passed** |
| `Workshop.LocalModel.Tests` (offline) | 22 passed / 5 skipped | 22 passed / 5 skipped |
| Root `dotnet test` total | 151 passed, 5 skipped (156) | **31 passed, 5 skipped (36)** |
| `Workshop.LocalModel.Tests` with a model | not run before | **27 passed / 0 failed, 13 s** |

120 of the 129 core tests were legacy. The 9 that remain are `CrashWorkflowTests` (4),
`IncidentDatasetTests` (3) and `VictoriaRoadCrashCorpusTests` (2).

Model-backed command used, against Nemotron already loaded in LM Studio:

```
WORKSHOP_LOCAL_MODEL=1 MAF_ENDPOINT=http://localhost:1234/v1 \
MAF_MODEL=nvidia-nemotron-3-nano-4b MAF_API_KEY=x dotnet test tests/Workshop.LocalModel.Tests
→ Passed! Failed: 0, Passed: 27, Skipped: 0, Total: 27, Duration: 13 s
```

## Portability check

`starter/` was regenerated, copied to `/tmp/portability-check` with `bin/`+`obj/` stripped, and tested there:

```
/tmp/portability-check$ dotnet test
→ Workshop.Core.Tests:      Failed: 0, Passed:  9
→ Workshop.LocalModel.Tests: Failed: 0, Passed: 22, Skipped: 5
```

**Zero `DirectoryNotFoundException`.** Same result for a copied `solution/` at
`/tmp/portability-check-solution` (9 + 22). No test-code change was needed: the corpus test already
loads from `AppContext.BaseDirectory/workshop-data/…` (copied to output by the csproj) and
`LocalModelTests` resolves through `WorkshopPaths`, which walks up to `Directory.Packages.props` —
present at the root of a standalone `starter/` copy. The 87 failures came purely from the deleted
legacy tests walking up for `evidence-pack/`.

With a model, the copied starter reports 26 passed / **1 failed** — `SupportedTermSelectsRecordsAndProducesAFinding`
expecting `Supported`, getting `UnsupportedSelection`. That is the intended unfinished-TODO signal, not a path problem.

## Deleted

### Canonical source (`src/`)
| File | Reason |
|---|---|
| `src/Workshop.Core/ClaimLedger.cs` | claim-ledger build; no reference from the crash pipeline |
| `src/Workshop.Core/Verifier.cs` | the 12 R-rules; no reference |
| `src/Workshop.Core/VerificationReport.cs` | verifier result types; no reference |
| `src/Workshop.Core/BriefRenderer.cs` | rendered `incident-brief.md`; no reference |
| `src/Workshop.Core/SourceFacts.cs` | parsed the fictional evidence pack; no reference |
| `src/Workshop.Core/LedgerBuilder.cs` | built the claim ledger; no reference |
| `src/Workshop.Core/ClaimSemantics.cs` | R12 kind-semantics; no reference |
| `src/Workshop.Core/DefectInjector.cs` | the 6 seeded defects; no reference |
| `src/Workshop.Core/EvidenceStore.cs` | whitelisted file reader for `evidence-pack/`; no reference |
| `src/Workshop.Core/TextNormalization.cs` | quote-matching helper used only by `Verifier`/`BriefRenderer` |
| `src/Workshop.App/EvidenceToolHost.cs` | old tool host; already excluded via `SKIP_FILES` |
| `src/Workshop.App/IncidentPipeline.cs` | old pipeline (carried TODO 2 and 3); already excluded via `SKIP_FILES` |

Kept in `Workshop.Core`: `CrashWorkflow.cs`, `IncidentDataset.cs`, `WorkshopJson.cs`
(`WorkshopJson` is used by `Program.cs`, `CrashPipeline.cs` and `IncidentDataset.cs`).

### Tests (`tests/Workshop.Core.Tests/`)
`VerifierTests.cs`, `BriefRendererTests.cs`, `SeededDefectTests.cs`, `LedgerBuilderTests.cs`,
`ClaimSemanticsTests.cs`, `EvidenceStoreTests.cs`, `SourceFactsTests.cs` — all cover deleted types.

`TestFixtures.cs` — deleted whole rather than trimmed: every member was legacy (`EvidenceDir`,
`Store()`, `Facts()`, `CleanLedger()`, `Verify()`). Its `EvidenceDir` walk-up was the source of the
87 `DirectoryNotFoundException`s. Nothing that remains referenced it.

### Data and scripts
| Path | Reason |
|---|---|
| `evidence-pack/` (5 files) | the fictional incident pack; nothing in the crash pipeline reads it |
| `scripts/demo-break-it.sh` | drove `verify-only --inject-defect` over the 6 seeded defects; both commands are gone |
| `scripts/demo-clean-run.sh` | asserted the three legacy artefacts and `evidence-pack/` contents |
| `artifacts/break-it/`, `artifacts/{claim-ledger.json,verification.json,incident-brief.md,gate-report.json}` | legacy run output; git-ignored, so invisible in the stat |

## Edited

| File | Change |
|---|---|
| `scripts/generate-starter.py` | `SKIP_FILES` reduced to `set()` (two entries pointed at deleted files; see judgment call 1 for the third) |
| `scripts/verify-all.sh` | dropped both demo checks; replaced the unusable `gates --repeat 5` with `typed`, `ready --term intersection` and the model-backed test project; replaced the "starter/ is red" check (see judgment call 2); added the two Gather checks |
| `scripts/check-distribution.sh` | replaced `verify-only --ledger workshop/reference-run/claim-ledger.json` + three-artefact assertion with `gather --term intersection` and a records assertion |
| `scripts/offline-proof.sh` | dropped the three-artefact loop and the `demo-break-it.sh` call; now proves Gather, `smoke`, `run` and `ready` offline |
| `scripts/reset-workshop.sh` | dropped `evidence-pack/` from the checkout and the named legacy artefacts from the delete list |
| `scripts/rehearse-60.sh` → `scripts/rehearse-85.sh` | rewritten for the new path (see judgment call 3) |
| `scripts/prefetch.sh` | `ready` → `ready --term intersection`, matching SETUP.md |
| `workshop/SETUP.md` | `# expect 129 core tests passed` → `# expect 31 passed (5 local-model tests skipped)`; "Only the **fictional evidence pack**" → "Only the **bundled Victorian crash sample**" |
| `workshop/REHEARSAL.md` | "129 deterministic core tests pass" → "9 deterministic core tests and 22 offline model-lane tests pass" |
| `starter/`, `solution/` | regenerated; `python3 scripts/generate-starter.py --check` reports no drift |

Nothing else in the doc set needed a change. `README.md`, `AGENTS.md`, `CLAUDE.md`,
`workshop/slides/index.html`, `CHECKPOINTS.md`, `DEMOS.md`, `AGENDA-90.md`, `FACILITATOR-RUNBOOK.md`,
`RECOVERY-CARD.md`, `ORGANISER.md`, `HYBRID-DELIVERY-CARD.md`, `ATTENDEE-GUIDE.md`, `GLOSSARY.md`
and `ADVANCED-OPENAI-COMPATIBLE-RECOVERY.md` were already free of stale counts and legacy artefacts.
The deck carries **no** test count or legacy artefact string, so it was not touched.

Banner-marked historical files left alone as instructed: `CLAIMS-AND-LIMITS.md`, `AGENDA-60.md`,
`AGENDA-120.md`, `LM-STUDIO-SWEEP.md`. `workshop/reviews/*` were also left alone — they are dated
review records, not live guidance.

## Kept on purpose

- **`workshop/data/synthetic-incident-records.json`** — not legacy. It matches the current
  `IncidentRecord` schema, was created for this build on 29 Aug, and `workshop/data/README.md`
  documents it as the `--dataset` recovery fallback.
- **`scripts/prefetch.sh`, `scripts/check-script-modes.sh`** — build/runtime generic, nothing stale.
- **`scripts/offline-proof.sh`** — offline operation is the workshop's premise, so it was fixed, not deleted.
- **`workshop/reference-run/`** — `gather-intersection.json` is the current build's saved output.
  (`claim-ledger.json`, `verification.json`, `incident-brief.md`, `gate-report*.json` were already
  deleted in the working tree before this task started; they appear in the stat as pre-existing deletions.)
- **TODO numbering stays 4 and 5.** With `IncidentPipeline.cs` gone there is no TODO 1–3, so the
  generated READMEs read "TODO 4, TODO 5" with no 1–3. This gap pre-dates this cleanup (TODO 1 was
  already gone and 2–3 were already `SKIP_FILES`-excluded). Renumbering would touch the generator,
  both generated trees and the agenda docs — out of scope here, but worth a follow-up.

## Judgment calls

1. **`synthetic-incident-records.json` removed from `SKIP_FILES`, so it now ships in `starter/` and
   `solution/`.** Strictly only the two `.cs` entries were dead. But the generated trees carry a
   `workshop/data/README.md` that advertises this file as the recovery fallback while the file was
   excluded — the attendee tree contradicted its own README. Including it (6 records, ~1 KB) is the
   smaller fix. Drop this hunk if you would rather cut the README paragraph instead.

2. **`verify-all.sh`'s "starter/ is red before the TODOs" check had to change.** With the legacy
   tests gone, the starter's deterministic suite is **green** (31 passed): both TODOs live in
   `Workshop.App`, and the deterministic tests only cover `Workshop.Core`. The old red signal was
   entirely the 87 missing-`evidence-pack/` errors — a broken check, not a teaching signal. It is
   now: `dotnet run --project starter/src/Workshop.App -- run --term intersection` must exit non-zero.
   Verified it exits **2** (`gate: UnsupportedSelection`) with **no model running** — the stub returns
   `null` before any call — and that the same command on `solution/` exits **0** with a model, so the
   check can actually fail. Worth telling facilitators: the starter is no longer red on `dotnet test`;
   its incompleteness now shows as the caution gate (and, under `WORKSHOP_LOCAL_MODEL=1`, one failing
   model-backed test).

3. **`rehearse-60.sh` renamed to `rehearse-85.sh` and rewritten.** The 60-minute agenda is archived;
   the live path is the 85-minute one. No doc, script or deck referenced the old filename. The body
   now times: restore → cold build → test (31/0) → both Gather commands → the pre-TODO caution branch
   (exit 2) → copy `solution/src/Workshop.App/CrashPipeline.cs` → build → test → model-backed `run`
   and `ready` (skippable with `SKIP_MODEL=1`). Two mechanical fixes were needed along the way:
   `dotnet run … --no-build` resolved the Debug output and failed with a missing `Workshop.Core`
   assembly (reverted to the original script's plain `dotnet run`), and back-to-back implicit restores
   in one tree hit an SDK error (`project.assets.json already exists`), so the build and test steps
   now pass `--no-restore`.

## Verification run

```
$ dotnet test Workshop.slnx
  Workshop.Core.Tests:       Failed: 0, Passed:  9
  Workshop.LocalModel.Tests: Failed: 0, Passed: 22, Skipped: 5

$ python3 scripts/generate-starter.py --check
  no drift: starter/ and solution/ match a fresh generation of src/

$ SKIP_PACKAGING=1 MAF_ENDPOINT=http://localhost:1234/v1 \
  MAF_MODEL=nvidia-nemotron-3-nano-4b MAF_API_KEY=x bash scripts/verify-all.sh
  … 12 checks … VERIFY_ALL: PASS   (model lane included: smoke, typed, ready, model-backed tests)

$ MAF_ENDPOINT=… bash scripts/rehearse-85.sh
  REHEARSE_85: PASS   (16.4 s mechanical total)

$ bash scripts/check-script-modes.sh
  SCRIPT MODES: PASS (7 scripts)
```

Forced failures, to show the checks can fail: appending a line to
`starter/src/Workshop.Core/CrashWorkflow.cs` made `--check` print `DRIFT DETECTED` (exit 1); the
`solution/` run exits 0 where the starter exits 2; and an earlier `rehearse-85.sh` run reported
`REHEARSE_85: FAIL (5 unexpected outcome(s))` before the `--no-build` fix.

**Not verified:** `scripts/check-distribution.sh` (both modes) and `verify-all.sh`'s packaging
block. They clone/archive `HEAD`, and this change set is deliberately staged-not-committed, so a
clone would test the pre-cleanup tree. Run them after committing. `offline-proof.sh` was also not
run — it needs root and Linux `iptables`; it was edited by inspection only.

## The stat

`CLEANUP-STAT.txt` is `git diff --cached --stat` against the last commit (`abd6595`). It is a noisy
inventory: the copy carried a large amount of pre-existing uncommitted pivot work (the deck, the
review PNGs, the `solution/` deletions, the `workshop/reference-run/` deletions), so the 565-file
total is **not** this cleanup. The exact change set produced here is:

```
diff -rq --exclude=.git --exclude=bin --exclude=obj <original> /tmp/cleanup-repo
```

which yields 8 modified files, 1 rename, 2 added files (the synthetic dataset into the two generated
trees), and the deletions listed above.
