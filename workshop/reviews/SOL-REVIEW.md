# Sol high — independent third-party review

**Verdict as received: `revise` / status PARTIAL.**

Sol reviewed the implementation independently of the Fable lane. Its ten must-fixes and its
additional notes are recorded here with the exact disposition of each. Where Sol proposed a specific
remedy, the remedy actually implemented is stated — including where it differs.

## Must-fixes

| # | Must-fix | Disposition | Evidence |
| --- | --- | --- | --- |
| 1 | Verifier checks free-form events only for source and quote, not value entailment; a cause can evade `R9` by being mislabelled as an event. Add a deterministic, defensible semantic policy with tests and defects, and calibrate public wording. No LLM judge | **Accepted and implemented as proposed.** `ClaimSemantics` canonicalizes text to whole tokens; `R12-KIND-SEMANTICS` detects causal markers in a claim's value *and* quotes and fails when the declared kind is not `cause`; `R9` now fires on detected semantics, not the label; `R11-EVENT-SUPPORTED` grades event claims against event phrases parsed from `events.csv` and marks unsupported free text `UNVERIFIED`. All token comparison, no model | Evasion reproduced before the fix (22 passed / 0 failed / exit 0, claim in Verified facts) and closed after. 27 `VerifierTests`, 14 `ClaimSemanticsTests`, seeded defects `mislabelled-cause` and `unsupported-event` |
| 2 | Make distribution real: local git, readiness artifacts, executable scripts, clean clone/archive build and test, replace broken git-checkout recovery | **Accepted in full** | `git init` (no remote), 154 files committed, scripts `100755` in the index, `check-distribution.sh clone` and `… archive` both PASS, `reset-workshop.sh` replaces `git checkout artifacts/` |
| 3 | Replace smoke-only attendee readiness with an integrated run checking all three artifacts and a practical threshold | **Accepted in full** | New `ready` command: tool contract, semantic correctness, no failures, all three artifacts present, mutual consistency, and a 90 s budget. Wired into `SETUP.md`, `prefetch.sh`, `offline-proof.sh`, `verify-all.sh`. Forced-failure exit 6 demonstrated |
| 4 | Fix or remove hosted recovery. Implement the `AzureCliCredential` config seam if it can be done cleanly without authenticating; otherwise label design-only and remove dead flags | **Removed, not implemented — with a repair rather than a weakening.** `Azure.Identity` is absent from the NuGet cache, so implementing it would add a fresh network dependency to attendee prefetch for a lane with no live call. But the recovery lane itself needed no code at all: Azure OpenAI's `/openai/v1` route is OpenAI-compatible and key-authenticated, which the existing client already speaks. So the lane is now **documented as working by configuration**, `MAF_AUTH` is deleted, and Entra is labelled not implemented | Primary source checked 2026-08-14 (MS Learn, Azure OpenAI SDK language support). Lane resolution unit-tested in `ModelSettingsTests` (19 tests, always run). No `az login` attempted |
| 5 | Correct the 14/31 cue and the one-hour status | **Accepted in full** | 14 filtered / 80 whole-suite corrected in `ATTENDEE-GUIDE`, `AGENDA-60`, `AGENDA-120`. One-hour status now reads "credible, not proven — awaiting non-author rehearsal" in `README`, `ORGANISER`, `REHEARSAL`, `CLAIMS-AND-LIMITS` |
| 6 | Gate contract: L2 semantic correctness; each seeded defect rejected 3/3, not once | **Accepted in full** | L2 now requires required kinds present *and* correct against source parsing, every quote real, and no kind mislabelled — it reports named shortfalls when it fails. L5 runs six defects × 3 attempts and requires 18/18 |
| 7 | Break-it must save mutually consistent corrupted ledger/report/brief in isolated per-run directories, never overwriting clean evidence | **Accepted in full** | `verify-only --inject-defect` writes all three files together, defaults to `artifacts/break-it/<defect>/`, and takes `--ledger` so the clean ledger is only read. `demo-break-it.sh` uses per-run directories and asserts the clean ledger's hash is unchanged |
| 8 | Scripts must fail honestly: `set -euo pipefail`, assert expected codes and results, no stale artifacts | **Accepted in full** | All nine scripts. `check-script-modes.sh` enforces shebang, executable bit and `set -euo pipefail` as a gate. `verify-all.sh` gained `check_fails` for steps whose failure is the point |
| 9 | Correct authorship: the model proposes claims; code attaches, normalizes, merges and serializes | **Accepted in full** | README artifact table, `GLOSSARY` claim-ledger entry, `CLAIMS-AND-LIMITS` design-choices section |
| 10 | Persist review lane results and Opus disposition | **Accepted in full** | This file, [FABLE-VERIFICATION.md](FABLE-VERIFICATION.md), and [OPUS-DISPOSITION.md](OPUS-DISPOSITION.md) |

## Additional notes

| Note | Disposition | Evidence |
| --- | --- | --- |
| Call it a deterministic C# pipeline around MAF agents/tools, not the MAF Workflow API | **Accepted.** README, `GLOSSARY`, `CLAIMS-AND-LIMITS`. The unreferenced `Microsoft.Agents.AI.Workflows` pin is deleted rather than re-commented | `Directory.Packages.props` |
| Gate artifact provenance: model digest, Ollama version, settings, hardware, date, placement; qualify unmeasured hardware claims | **Accepted.** A `provenance` block is captured into every gate report from Ollama's own API and the local machine | `gate-report.json`. The GPU model, VRAM and driver named in earlier drafts were **not measurable** (`nvidia-smi` absent) and have been removed; placement is measured and kept |
| Derive LOCAL/HOSTED from config; bounded request timeout enforcing 90 s | **Accepted.** Lane is derived from the endpoint (loopback ⇒ LOCAL) and printed on every run; every model call is cancelled at `MAF_TIMEOUT_SECONDS`, default 90, clamped 5–600 | `ModelSettingsTests`; forced failure aborted at 5 s with exit 3 rather than sitting through a 30 s stall |
| Primary source links and research dates for framework/runtime claims | **Accepted, and it caught two errors.** Re-reading the sources showed the repo's claim that Ollama supports `tool_choice` was wrong (the docs list it unsupported), and that the "auto-enables thinking" claim was not stated by the cited page at all — it is now filed as INFERRED | `CLAIMS-AND-LIMITS` DOCUMENTED table, all links dated 2026-08-14 |
| Prefetch Qwen only if the optional comparison is retained | **Accepted.** Comparison retained; `prefetch.sh` pulls it only under `WORKSHOP_PREFETCH_QWEN=1` | `prefetch.sh`; listed UNVERIFIED |
| Normal run must fail when the tool contract is broken; L2 cannot be non-empty + incident-only | **Accepted.** `run` exits 5 with a named failure; L2 rebuilt as described in must-fix 6 | Forced failure against a tool-less endpoint |
| prefetch/demo scripts must assert outcomes | **Accepted.** Both demo scripts assert shape, exit codes, rule identity, artifact consistency and clean-ledger integrity, and exit non-zero on any mismatch | `demo-clean-run.sh`, `demo-break-it.sh` |

## Where the implementation differs from Sol's suggested remedy

One place, must-fix 4. Sol offered "implement the credential seam, **or** label design-only and
remove the dead flags". The second branch was taken, but with an addition Sol did not specify: the
recovery lane was found to already work by key against an OpenAI-compatible endpoint, so rather than
documenting a downgraded promise, the *working* path is now the documented one and only the Entra
variant is labelled unimplemented. This is recorded here because it is a judgement call, not a
mechanical application of the review.

## Not addressed by either lane, found during the correction

- A quote spliced from two different source lines passed `R2-QUOTE-PRESENT`. Fixed by scoping quote
  matching to one physical line; now the `spliced-quote` seeded defect.
- `verify-only --inject-defect` previously left the tree describing two different runs.
- Two DOCUMENTED claims about the Ollama runtime were wrong. See the additional-notes table.
