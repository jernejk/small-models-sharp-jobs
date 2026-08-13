# Rehearsal report

Reference machine, 14 August 2026. Full hardware and runtime detail in
[CLAIMS-AND-LIMITS.md](CLAIMS-AND-LIMITS.md).

## Headline

**The machine is not the bottleneck.** The entire 60-minute path costs **35.6 seconds** of waiting,
and 23.4 s of that is the one model run at the end. Everything else in the hour is human time:
reading, typing, and the facilitator explaining.

**A non-author human has not yet rehearsed this.** That is the one thing this report cannot supply,
and it is the gate on calling the 60-minute path proven rather than credible.

## 60-minute path, measured

`scripts/rehearse-60.sh`, from a clean `starter/` copy.

| Step | Wait | Result |
| --- | --- | --- |
| `dotnet restore` (warm NuGet cache) | 1.0 s | ok |
| `dotnet build` (first, cold) | 2.5 s | ok |
| `dotnet test` — the starting point | 1.5 s | 10 passed / 53 failed |
| `dotnet test` after TODO 1 | 2.6 s | 31 passed / 32 failed |
| `dotnet build` after TODO 2 + 3 | 1.4 s | ok |
| `dotnet test` after TODO 4 | 2.6 s | **63 passed / 0 failed** |
| `dotnet run -- run` | 23.4 s | three artifacts |
| `verify-only --inject-defect altered-number` | 0.6 s | exit 2, defect caught |
| **Total** | **35.6 s** | |

The test progression 10 → 31 → 63 is deliberate: attendees get visible green twice before the final
run, so nobody spends 40 minutes unsure whether anything works.

**Reading of this:** ~54 minutes of the hour are available for human work across four small edits,
with a documented copy-from-`solution/` escape at every step. That is why the agenda is judged
credible. It is **not** a substitute for watching a real person do it.

## Local model gates

`Workshop.App gates --repeat 5`, `nemotron-3-nano:4b` on Ollama 0.32.9, 4096 context,
non-streaming, temperature 0, reasoning effort none.

| Gate | Result |
| --- | --- |
| L1 smoke — exact `JACKDAW_OK` | PASS |
| L2 typed extraction, schema-valid and semantically correct | 5/5 |
| L3 tool contract, correct calls and arguments | 5/5 |
| L4 integrated pipeline, zero verification failures | 5/5 |
| L5 seeded defects, each on its intended rule | 3/3 |
| L6 warm full path ≤ 30 s | PASS |
| L6b no run > 90 s | PASS |

Per-run wall clock: **23.56, 21.76, 24.58, 25.14, 24.54 s** — median 24.5 s, worst 25.1 s.
An earlier gate run the same day gave median 23.9 s, worst 24.8 s, so expect roughly 22–25 s.

All five runs produced **identical** output: 8 claims, 33 passed, 0 failed, 1 unverified. Model
placement was 30% CPU / 70% GPU at 4096 context throughout.

Defects, each failing on exactly one rule and the intended one:

| Defect | Expected | Observed |
| --- | --- | --- |
| phantom source | `R1-SOURCE-WHITELIST` | `R1-SOURCE-WHITELIST` |
| altered number | `R5-AFFECTED-CUSTOMERS` | `R5-AFFECTED-CUSTOMERS` |
| altered timestamp | `R6-TIMESTAMP` | `R6-TIMESTAMP` |

## Everything else that was run

| Check | Result |
| --- | --- |
| `scripts/verify-all.sh` | `VERIFY_ALL: PASS` (9 checks) |
| Deterministic tests | 63/63, no model |
| `starter/` compiles and is red; `solution/` compiles and is green | PASS |
| No drift between `src/` and the generated trees | PASS |
| Offline Tier A — no non-loopback egress, cache-only restore, three artifacts | `OFFLINE_PROOF: PASS` |
| Demo scripts (`demo-clean-run.sh`, `demo-break-it.sh`) | run clean |

## Failures found and fixed during the build

Recorded because they are the interesting part, and because two of them are now teaching material.

1. **Structured output + tools returns nothing.** One agent call offering both returned
   `{"claims": []}` in ~1.4 s and never invoked the tool — silently, no error. Fixed by splitting
   into a tool-enabled gather call and a tool-free typed extraction call. Now DEMOS.md demo 3.
2. **Agent state leaked across sources.** A reused extraction agent carried `status.txt` facts into
   the `customer-email.txt` extraction, emitting `incident_id = "SEV-2"` and `severity = "7"` cited
   to the email. Fixed with a fresh agent per source: 7 verification failures → 0.
3. **Four evidence files blew the latency budget** — projected 39.8 s against a 30 s gate. Fixed by
   sending only the two prose files to the model and parsing `events.csv` in code. 24.5 s median.
4. **The evidence pack itself broke the gate.** An earlier `customer-email.txt` said "Seven of our
   sites" and "this morning"; the model correctly extracted both, and both correctly failed their
   rules. Rewritten so the email carries impact and an unsupported cause only.
5. **Three tests asserted C# record equality on a collection member**, which compares by reference.
   Rewritten to compare serialized bytes — which is what the artifacts actually need anyway.

## Not rehearsed

- **A non-author completing the 60-minute core by minute 55.** The blocking item.
- **A full 120-minute delivery**, including the extension segments.
- **Tier B offline** — physically disconnecting Wi-Fi rather than firewalling egress.
- **Any machine other than the reference machine**, including native Windows and Apple Silicon.
- **LM Studio**, the hosted recovery lane, and the free cloud lane.
- **A room of attendees prefetching on venue Wi-Fi**, which remains the largest logistical risk.

## Recommended before 31 August

1. **Have someone who did not write this sit the 60-minute path**, timed, with no help. This is the
   only item that changes the workshop's status.
2. Send [SETUP.md](SETUP.md) one week out and again 48 hours before.
3. Get the recovery-lane decision closed (Azure deployment, or accept pairing).
4. Do one Tier B offline run — flight mode, full path, three artifacts.
5. If LM Studio is wanted as a blessed runtime, run `gates --repeat 5` against it. Until then it
   stays a compatibility target.
