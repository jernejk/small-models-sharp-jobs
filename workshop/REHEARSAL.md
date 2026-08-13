# Rehearsal report

Reference machine, 14 August 2026. Full hardware and runtime detail in
[CLAIMS-AND-LIMITS.md](CLAIMS-AND-LIMITS.md); the same detail is captured automatically into
[reference-run/gate-report.json](reference-run/gate-report.json) under `provenance`.

## Headline

**The machine is not the bottleneck.** The entire 60-minute path costs **31.7 seconds** of waiting,
and 20.4 s of that is the one model run at the end. Everything else in the hour is human time:
reading, typing, and the facilitator explaining.

**A non-author human has not yet rehearsed this.** That is the one thing this report cannot supply,
and it is the gate on calling the 60-minute path proven rather than credible. **Status: awaiting a
non-author rehearsal.**

## 60-minute path, measured

`scripts/rehearse-60.sh`, from a clean `starter/` copy. Every step asserts its expected exit code
and test counts, so this script fails rather than quietly reporting a changed progression.

| Step | Wait | Result |
| --- | --- | --- |
| `dotnet restore` (warm NuGet cache) | 1.2 s | ok |
| `dotnet build` (first, cold) | 1.0 s | ok |
| `dotnet test` — the starting point | 2.5 s | 52 passed / 87 failed |
| `dotnet test` after TODO 1 | 2.4 s | 80 passed / 59 failed |
| `dotnet build` after TODO 2 + 3 | 1.3 s | ok |
| `dotnet test` after TODO 4 | 2.5 s | **139 passed / 0 failed** |
| `dotnet run -- run` | 20.4 s | three artifacts |
| `verify-only --inject-defect altered-number` | 0.6 s | exit 2, defect caught |
| **Total** | **31.7 s** | |

The test progression 52 → 80 → 139 is deliberate: attendees get visible green twice before the final
run, so nobody spends 40 minutes unsure whether anything works.

**Reading of this:** ~55 minutes of the hour are available for human work across four small edits,
with a documented copy-from-`solution/` escape at every step. That is why the agenda is judged
credible. It is **not** a substitute for watching a real person do it.

## Local model gates

`Workshop.App gates --repeat 5`, `nemotron-3-nano:4b` (digest `6cc467f054393a55`, Q4_K_M) on Ollama
0.32.9, non-streaming, temperature 0, reasoning effort none, 90 s per-call ceiling. Run twice.

| Gate | Result |
| --- | --- |
| L1 smoke — exact `JACKDAW_OK` | PASS |
| L2 semantic extraction — required kinds present *and* correct, every quote real, no kind mislabelled | 5/5, twice |
| L3 tool contract, correct calls and arguments | 5/5, twice |
| L4 integrated pipeline, zero verification failures | 5/5, twice |
| L5 seeded defects — each of six rejected on its intended rule, three times each | 6 × 3/3, twice |
| L6 warm full path ≤ 30 s | PASS |
| L6b no run > 90 s, ceiling enforced by cancellation | PASS |

Per-run wall clock: **19.4, 19.9, 22.0, 22.0, 20.9 s** (median 20.9, worst 22.0) and **19.6, 19.2,
19.6, 20.0, 19.8 s** (median 19.6, worst 20.0). Expect roughly **19–22 s** warm.

All ten runs produced **identical** output: 7 claims, 30 passed, 0 failed, 2 unverified. Placement
was 30% CPU / 70% GPU throughout, read from Ollama rather than asserted.

Defects, each rejected on exactly one rule and the intended one, three attempts each:

| Defect | Expected | Observed |
| --- | --- | --- |
| phantom source | `R1-SOURCE-WHITELIST` FAIL | 3/3 |
| altered number | `R5-AFFECTED-CUSTOMERS` FAIL | 3/3 |
| altered timestamp | `R6-TIMESTAMP` FAIL | 3/3 |
| mislabelled cause | `R12-KIND-SEMANTICS` FAIL | 3/3 |
| spliced quote | `R2-QUOTE-PRESENT` FAIL | 3/3 |
| unsupported event | `R11-EVENT-SUPPORTED` UNVERIFIED | 3/3 |

The last one is the interesting case: it produces **no** failure and exit 0. It is still rejected,
because rejection here means "never reached Verified facts", not "made something go red".

## Forced failures

A check that cannot fail is not a check. Each of these was induced deliberately:

| Induced | Result |
| --- | --- |
| Endpoint that answers but never calls the tool | `TOOL CONTRACT BROKEN`, **exit 5** |
| Endpoint that sleeps 30 s with `MAF_TIMEOUT_SECONDS=5` | aborts at 5 s, **exit 3**, ceiling enforced not observed |
| `ready` against the tool-less endpoint | three named failures, **exit 6** |
| A ledger with a causal claim relabelled `event` | `R12-KIND-SEMANTICS` FAIL, kept out of Verified facts |
| A quote spliced from two source lines | `R2-QUOTE-PRESENT` FAIL |
| `verify-all.sh` with a deliberately broken tree | `VERIFY_ALL: FAIL`, non-zero exit |

## Everything else that was run

| Check | Result |
| --- | --- |
| `scripts/verify-all.sh` | `VERIFY_ALL: PASS` |
| Deterministic tests | 139/139, no model |
| `starter/` compiles and is red; `solution/` compiles and is green | PASS |
| No drift between `src/` and the generated trees | PASS |
| Offline Tier A — no non-loopback egress, cache-only restore, three artifacts | `OFFLINE_PROOF: PASS` |
| Clean `git clone` builds, tests and runs | `DISTRIBUTION (clone): PASS` |
| `git archive` of HEAD builds, tests and runs | `DISTRIBUTION (archive): PASS` |
| Scripts executable in the git index, shebanged, `set -euo pipefail` | `SCRIPT MODES: PASS` |
| Demo scripts (`demo-clean-run.sh`, `demo-break-it.sh`) | pass, and fail loudly when their assertions do not hold |

## Failures found and fixed during the build

Recorded because they are the interesting part, and because several are now teaching material.

1. **A mislabelled cause passed every rule.** Labelling the customer's line *"the outage was caused
   by the new billing system"* as kind `event` produced 22 passed / 0 failed / exit 0, and put the
   speculation straight into **Verified facts** — the exact thing `runbook.md` forbids. Fixed with
   kind-independent causal detection (`R12`) plus `R9` applying on semantics rather than on the
   label. Now seeded defect `mislabelled-cause` and DEMOS.md demo 2b.
2. **A quote spliced from two source lines passed `R2`.** Whole-file normalization joined unrelated
   lines. `R2` is now scoped to a single physical line, which is what the extraction prompt asked
   for all along. Now seeded defect `spliced-quote`.
3. **Structured output + tools returns nothing.** One agent call offering both returned
   `{"claims": []}` in ~1.4 s and never invoked the tool — silently, no error. Fixed by splitting
   into a tool-enabled gather call and a tool-free typed extraction call. Now DEMOS.md demo 3.
4. **Agent state leaked across sources.** A reused extraction agent carried `status.txt` facts into
   the `customer-email.txt` extraction, emitting `incident_id = "SEV-2"` and `severity = "7"` cited
   to the email. Fixed with a fresh agent per source: 7 verification failures → 0.
5. **A broken tool contract exited 0.** `run` reported a clean run when the agent had fetched
   nothing. It now exits 5 and says so.
6. **The 90 s ceiling was only observed after the fact.** There was no timeout, so a stalled runtime
   would hang rather than fail. Every model call is now cancelled at the budget.
7. **Break-it left the tree inconsistent.** `verify-only --inject-defect` rewrote
   `verification.json` and `incident-brief.md` from a corrupted ledger while leaving the clean
   `claim-ledger.json` in place — three files describing two different runs. Each defect now writes
   a complete trio into its own directory and the clean ledger is only ever read.
8. **Four evidence files blew the latency budget** — projected 39.8 s against a 30 s gate. Fixed by
   sending only the two prose files to the model and parsing `events.csv` in code.
9. **The evidence pack itself broke the gate.** An earlier `customer-email.txt` said "Seven of our
   sites" and "this morning"; the model correctly extracted both, and both correctly failed their
   rules. Rewritten so the email carries impact and an unsupported cause only.
10. **Three tests asserted C# record equality on a collection member**, which compares by reference.
    Rewritten to compare serialized bytes — which is what the artifacts actually need anyway.
11. **There was no repository.** The tree was a directory: no commit, scripts not executable, and a
    recovery card telling facilitators to run `git checkout`. Now a local git repository whose clean
    clone and archive both build and test.

## Not rehearsed

- **A non-author completing the 60-minute core by minute 55.** The blocking item.
- **A full 120-minute delivery**, including the extension segments.
- **Tier B offline** — physically disconnecting Wi-Fi rather than firewalling egress.
- **Any machine other than the reference machine**, including native Windows and Apple Silicon.
- **LM Studio**, and any live hosted call.

## Recommended before 31 August

1. **Have someone who did not write this sit the 60-minute path**, timed, with no help. This is the
   only item that changes the workshop's status.
2. Send [SETUP.md](SETUP.md) one week out and again 48 hours before.
3. Get the recovery-lane decision closed (organiser endpoint and key, or accept pairing).
4. Do one Tier B offline run — flight mode, full path, three artifacts.
5. If LM Studio is wanted as a blessed runtime, run `gates --repeat 5` against it. Until then it
   stays a compatibility target.
