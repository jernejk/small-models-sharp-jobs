# Claims and limits

Every factual claim this workshop makes, sorted by how we know it. Read this before repeating any
number from this repo.

- **MEASURED** — observed by running it on the reference machine, with the command that produced it.
- **DOCUMENTED** — stated by a primary source, with a link and the date we checked it.
- **INFERRED** — reasoned from the above. Not observed. Could be wrong.
- **UNVERIFIED** — not run. Listed so nobody assumes it was.

## Reference machine

All MEASURED numbers come from one machine, on 14 August 2026:

```text
Dell XPS 15 9520, Ubuntu 24.04.4 LTS under WSL2
NVIDIA RTX 3050 Ti Laptop GPU, 4096 MiB VRAM, driver 596.08
.NET SDK 10.0.302 · Ollama 0.32.9
model nemotron-3-nano:4b · context 4096 · non-streaming · temperature 0 · reasoning effort none
```

One machine is one data point. Nothing here claims a second machine behaves the same.

## MEASURED

| Claim | Evidence |
| --- | --- |
| Deterministic tests pass 63/63, no model required | `dotnet test` |
| Local gates all pass | `Workshop.App gates --repeat 5` → `OUTCOME: PASS` |
| Typed extraction 5/5, tool contract 5/5, integrated 5/5 | same gate run, 5 repetitions |
| Warm full path: median 24.5 s, worst 25.1 s | same gate run; every run inside the 30 s budget. A second gate run the same day gave median 23.9 s / worst 24.8 s, so expect ≈ 22–25 s |
| Split: gather ≈ 3.9 s, extract ≈ 18.6 s | `run` output timing line |
| Three seeded defects each fail on exactly their intended rule | gate run + `SeededDefectTests` |
| Model placement 30% CPU / 70% GPU at 4096 context | `ollama ps` during the gate run |
| Runs are reproducible: identical claims and counts across 5 runs | gate run, 8 claims / 33 pass / 1 unverified each time |
| Starter and solution both compile; no drift outside TODO regions | `generate-starter.py --check`, `dotnet build` on both |
| Starter is red (10 passed / 53 failed), solution green (63/63) | `dotnet test` on each tree |
| Deterministic tests genuinely fail when the code is broken | three mutants injected; caught 1, 7 and 2 tests respectively |
| Full path runs with **no non-loopback network**, restoring from local cache only | `scripts/offline-proof.sh` → `OFFLINE_PROOF: PASS` |
| 60-minute path costs 35.6 s of machine waiting end to end, of which 23.4 s is the single model run | `scripts/rehearse-60.sh` |
| Structured output **and** tools in one call returns `{"claims": []}` in ≈ 1.4 s, tool never invoked | isolated three-case probe; untyped+tools and typed-without-tools both worked |
| Sending all four evidence files through the model projects to ≈ 39.8 s, over budget | measured per-source extraction latency |
| A C# `enum` for claim kinds made semantic accuracy *worse* than a string plus an allowed-value list | probe: enum returned `Severity` for `7` and `Duration` for timestamps |

## DOCUMENTED

| Claim | Source | Checked |
| --- | --- | --- |
| `Microsoft.Agents.AI` 1.17.0 exists, published 2026-08-04, current latest | <https://www.nuget.org/packages/Microsoft.Agents.AI/1.17.0> | 2026-08-14 |
| Ollama's OpenAI-compatible endpoint supports `reasoning_effort` (including `none`) and `tool_choice` | <https://docs.ollama.com/api/openai-compatibility> | 2026-08-14 |
| Ollama auto-enables thinking on capable models when `reasoning_effort` is absent | same | 2026-08-14 |

That last one explains a failure recorded in the prior readiness work: a first run with default
reasoning returned an empty visible answer after 57.7 seconds. **Reasoning-off is a required
compatibility invariant here, not a tuning preference.**

## INFERRED

| Claim | Basis | Risk |
| --- | --- | --- |
| Apple Silicon will run the local lane | same runtime and SDK ship for arm64; unified memory suits a 4B model | not run once |
| Native Windows will run the local lane | same SDK and runtime | not run once |
| A non-author can finish the 60-minute core by minute 55 | ≈ 34 s machine time leaves ≈ 54 min of human time for four small edits, with staged checkpoints and a copy-from-solution escape | **a human has not sat this. See UNVERIFIED.** |
| Attendee first-run will be slower than 23.9 s | cold model load and cold JIT; gates measure warm runs | unquantified on attendee hardware |

## UNVERIFIED — not run, do not imply otherwise

- **A non-author human completing the one-hour path.** Not done. The 60-minute timing is a machine
  floor plus reasoning, not a rehearsal. Until someone who did not write this sits it end to end,
  treat the agenda as a plan.
- **LM Studio parity.** Not run against these gates. Templates and tool parsers differ between
  runtimes; passing on Ollama certifies nothing about LM Studio. It stays a compatibility target.
- **Controlled cloud (Azure OpenAI `gpt-5.4-mini`).** The provider seam is built and configuration-only,
  but no live call has been made. Requires a human `az login --use-device-code` and organiser
  deployment details. Schema and verifier semantics are unchanged by construction, **not by observation.**
- **Free cloud lane.** Not exercised by this application at all. Hosted free routes are throttled,
  mutable and possibly logged; fictional data only, never a workshop dependency.
- **Native Windows and macOS end-to-end.** See INFERRED.
- **Any hardware other than the reference machine.**
- **Qwen3.5 4B under this architecture.** Prior work found it passed typed extraction but dropped a
  required fact from prose. It has not been run through this deterministic-rendering pipeline.
- **Tier B offline proof** — physically disconnecting Wi-Fi. `offline-proof.sh` blocks non-loopback
  egress at the firewall for the workshop user, which is strong but not the same as no radio. A
  human should still do the physical test once.
- **Cold-start timing on attendee machines.**
- **Sustained multi-attendee load** on shared venue Wi-Fi during the prefetch step.

## Design choices worth stating plainly

**The model reads two of the four evidence files.** `status.txt` and `customer-email.txt` are prose,
which is a model's job. `events.csv` is structured data parsed exactly by code, and `runbook.md` is
policy. Sending all four projected to ≈ 39.8 s for a worse timeline than a CSV parser produces for
free. This is the workshop's thesis applied to itself: narrow jobs.

**`sourceId` is attached by code, not emitted by the model.** Extraction runs per source, so the
application already knows which file a claim came from; asking the model to repeat it adds tokens
and a failure mode. `R1-SOURCE-WHITELIST` therefore cannot fire from ordinary model drift in the
current pipeline — it guards the seeded phantom-source defect, hand-edited ledgers, and any future
multi-source extraction. Say this out loud rather than implying the model is being caught by it.

**"Proves its work" means the declared checks.** Evidence IDs, exact quotes, numbers, timestamps,
durations, required-claim presence, and refusing to assert causation. It does **not** mean proof of
every semantic claim, and it does not mean the brief is true. A claim can pass every rule and still
be a bad summary.

**We never ask a model to grade its own factual correctness.** All verification is ordinary code
comparing against text or against facts parsed independently from source. There is no LLM judge
anywhere in this repo.

**Typed output is not truth.** JSON grammar proves shape. That is all it proves.

## Prior work

The earlier readiness pack (`~/work/jackdaw-maf-offline-readiness`, 13 August 2026) measured a
*different* probe — a tool loop with a model-written brief. Its numbers, such as a 5.68 s tool-loop
median, describe that probe and **not** this application. Do not mix the two sets of figures.
