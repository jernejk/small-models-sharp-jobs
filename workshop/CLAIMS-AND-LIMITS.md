> Historical (pre-29-Aug pivot): describes the earlier incident-pack build. Current path: AGENDA-85.md.

# Claims and limits

Every factual claim this workshop makes, sorted by how we know it. Read this before repeating any
number from this repo.

- **MEASURED** — observed by running it on the reference machine, with the command that produced it.
- **DOCUMENTED** — stated by a primary source, with a link and the date we checked it.
- **INFERRED** — reasoned from the above. Not observed. Could be wrong.
- **UNVERIFIED** — not run. Listed so nobody assumes it was.

## Reference machine

All MEASURED numbers come from one machine, on 14 August 2026. Everything in this block except the
GPU line is captured automatically into every gate report under `provenance`, so it cannot drift
away from what actually ran:

```text
Ubuntu 24.04.4 LTS under WSL2 · x64 · 12th Gen Intel Core i7-12700H · 16 logical cores · 49 GB RAM
.NET SDK 10.0.302 (runtime 10.0.10) · Ollama 0.32.9
model nemotron-3-nano:4b · digest 6cc467f054393a55 · Q4_K_M · 4.0B
non-streaming · temperature 0 · reasoning effort none · context: runtime default (not set by us)
per-call request budget 90 s, enforced by cancellation
```

**The GPU is not enumerable from this environment.** `nvidia-smi` is not present under this WSL2
setup, so this repo makes **no verified claim about the GPU model, its VRAM or its driver version**.
What *is* measured is placement: Ollama reports 2,245,724,732 of 3,198,149,837 model bytes resident
in VRAM, i.e. **30% CPU / 70% GPU**. Earlier drafts of this file named a specific laptop GPU and
driver; those were not verifiable here and have been removed rather than repeated.

One machine is one data point. Nothing here claims a second machine behaves the same.

## MEASURED

| Claim | Evidence |
| --- | --- |
| Deterministic tests pass 142/142, no model required | `dotnet test Workshop.slnx` (120 core + 22 provider-seam; 4 model tests skipped by default) |
| Local gates all pass | `Workshop.App gates --repeat 5` → `OUTCOME: PASS`, twice |
| Semantic extraction 5/5, tool contract 5/5, integrated 5/5 | both gate runs, 5 repetitions each |
| Warm full path: median 19.6–20.9 s, worst 22.0 s | two gate runs the same day: 19.4/19.9/22.0/22.0/20.9 and 19.6/19.2/19.6/20.0/19.8. Expect roughly **19–22 s** warm |
| Split: gather ≈ 5.1 s, extract ≈ 15.7 s | `run` output timing line |
| Six seeded defects, each rejected 3/3 on exactly its intended rule | `gates` L5 + `SeededDefectTests` + `scripts/demo-break-it.sh` |
| Model placement 30% CPU / 70% GPU | Ollama `/api/ps`, captured into `gate-report.json` provenance |
| Runs are reproducible: identical claims and counts across 10 runs | both gate runs: 7 claims / 30 pass / 0 fail / 2 unverified every time |
| Starter and solution both compile; no drift outside TODO regions | `generate-starter.py --check`, `dotnet build` on both |
| Starter is red (55 passed / 87 failed), solution green (142/142) | `dotnet test` on each tree |
| Test progression 55 → 83 → 142 across the four TODOs | `scripts/rehearse-60.sh`, which asserts each checkpoint |
| `dotnet test --filter EvidenceStoreTests` gives **14 passed** | run on `solution/`. The 31 in earlier drafts was wrong; 80 is the whole-suite total after TODO 1 |
| Full path runs with **no non-loopback network**, restoring from local cache only | `scripts/offline-proof.sh` → `OFFLINE_PROOF: PASS` |
| 60-minute path costs 31.7 s of machine waiting end to end, of which 20.4 s is the single model run | `scripts/rehearse-60.sh` |
| A clean `git clone` and a `git archive` both build, test and run | `scripts/check-distribution.sh clone` and `… archive` |
| A mislabelled cause used to pass every rule and reach Verified facts | hand-built ledger labelling the customer's causal sentence `event`: 22 passed, 0 failed, exit 0 **before** R12; `R12-KIND-SEMANTICS` FAIL + `R9` UNVERIFIED after |
| A quote spliced from two different source lines used to pass `R2` | same probe; whole-file matching accepted it, line-scoped matching rejects it |
| A broken tool contract exits 5, not 0 | stub endpoint that never calls the tool → `TOOL CONTRACT BROKEN`, exit 5 |
| The 90 s per-call ceiling aborts the call | stub endpoint sleeping 30 s with `MAF_TIMEOUT_SECONDS=5` → aborts at 5 s, exit 3 |
| Structured output **and** tools in one call returns `{"claims": []}` in ≈ 1.4 s, tool never invoked | isolated three-case probe; untyped+tools and typed-without-tools both worked |
| Sending all four evidence files through the model projects to ≈ 39.8 s, over budget | measured per-source extraction latency |
| A C# `enum` for claim kinds made semantic accuracy *worse* than a string plus an allowed-value list | probe: enum returned `Severity` for `7` and `Duration` for timestamps |
| First run with default reasoning returned an empty visible answer after 57.7 s | prior readiness work; why `ReasoningEffort.None` is set |
| **LM Studio runs the full path**, on one machine, all gates green | `gates --repeat 5` via LM Studio 0.12.11 on an M4 Max, 24 Aug 2026 — see [LM-STUDIO-SWEEP.md](LM-STUDIO-SWEEP.md) |
| `nemotron-3-nano:4b` through LM Studio reproduces the Ollama output exactly: 7 claims / 30 pass / 0 fail / 2 unverified | same sweep, 5 runs |
| Of six local models, one fails the gates: `google/gemma-4-12b` scores L2 0/5 and L3 0/5 at a 94.6 s median | same sweep |
| Model size does not predict fitness — the fastest model (4.8 s) is the largest file (15.64 GB, 4B active); the slowest (94.6 s) is 6.77 GB | same sweep |
| `qwen/qwen3.8-27b` is not reproducible across repeats; every other model was byte-identical over 5 runs at temperature 0 | same sweep |
| **Apple Silicon runs the local lane**: 142/142 deterministic tests, full path, and `REHEARSE_60: PASS` on macOS arm64 | this repo on an M4 Max, 24 Aug 2026 |
| `gates` crashed against any non-Ollama runtime before the provenance fix — all gates green, then exit 3, no report | reproduced on LM Studio; closed by `ProvenanceTests` |
| `scripts/rehearse-60.sh` never worked on macOS: `grep -oP` is GNU-only, and `dotnet run --no-build` could not load `Workshop.Core` | both reproduced and fixed 24 Aug 2026 |
| **The full path completes with no GPU at all**, proven by placement rather than inferred from timing | reference machine (Jackdaw, commit `800c2dc`), 24 Aug 2026: `num_gpu 0` derived model, Ollama `/api/ps` reported `size_vram: 0`, re-checked after the run |
| CPU-only is **1.65× slower by median** (36.57 s vs 22.14 s) and 3.01× by worst run | `gates --repeat 5` on both placements, same machine, same day |
| CPU-only **fails the 30 s L6 gate on merit**, not on cold start — warm runs cluster at ~36 s | CPU runs: 68.17, 36.11, 36.63, 35.80, 36.57 s. Exit code 4; the 90 s ceiling was never breached |
| **Placement changes speed and nothing else.** GPU and CPU-only produced `claims=7 pass=30 fail=0 unver=2` on every run | same two gate runs; L1–L5 pass 5/5 in both |
| First run on CPU costs **68 s vs ~36 s warm** — a 1.9× cold-start penalty | same run; no comparable spike appeared on the GPU baseline |
| The GPU baseline independently reproduced at **22.14 s median** on the reference machine | consistent with the 19–22 s measured there on 14 Aug, a different build |

## DOCUMENTED

| Claim | Source | Checked |
| --- | --- | --- |
| `Microsoft.Agents.AI` 1.17.0 exists, published 2026-08-04, current latest | <https://www.nuget.org/packages/Microsoft.Agents.AI/1.17.0> | 2026-08-14 |
| Ollama's OpenAI-compatible endpoint supports `reasoning_effort`, including `none` | <https://docs.ollama.com/api/openai-compatibility> | 2026-08-14 |
| Ollama's OpenAI-compatible endpoint does **not** support `tool_choice` | same page, which lists it as unsupported | 2026-08-14 |
| Azure OpenAI exposes an OpenAI-compatible `https://<resource>.openai.azure.com/openai/v1/` base URL that works with the stock OpenAI client libraries, authenticated by either an API key or Entra ID | <https://learn.microsoft.com/en-us/azure/ai-foundry/openai/supported-languages> | 2026-08-14 |

**Two corrections to earlier drafts of this file.** They were found by re-reading the primary
sources rather than by anything failing:

1. An earlier row claimed Ollama's OpenAI-compatible endpoint supports `tool_choice`. The
   documentation lists it as **not supported**. Nothing in this repo sets `tool_choice`, so no
   behaviour changes — the claim was simply wrong.
2. An earlier row claimed, as DOCUMENTED, that Ollama auto-enables thinking when `reasoning_effort`
   is absent. That page says no such thing. The 57.7 s empty answer is MEASURED; the explanation
   for it is now filed under INFERRED, where it belongs.

## INFERRED

| Claim | Basis | Risk |
| --- | --- | --- |
| The 57.7 s empty answer was caused by reasoning being on by default | the same call with `ReasoningEffort.None` returns promptly and correctly; no primary source states the default | the mechanism is unconfirmed. **Reasoning-off is still a required compatibility invariant here** — that part is measured |
| Native Windows will run the local lane | same SDK and runtime | not run once |
| A non-author can finish the 60-minute core by minute 55 | ≈ 32 s machine time leaves ≈ 55 min of human time for four small edits, with staged checkpoints and a copy-from-solution escape | **a human has not sat this. See UNVERIFIED.** |
| Attendee first-run will be slower than 20 s | cold model load and cold JIT; gates measure warm runs | **now measured for the CPU-only case** (68 s cold vs ~36 s warm). Still unquantified on GPU and on attendee hardware generally |
| The hosted recovery lane works by changing three environment variables | the endpoint shape is documented above and the client is the stock OpenAI one; the seam is unit-tested | **no live hosted call has been made.** See UNVERIFIED |

## UNVERIFIED — not run, do not imply otherwise

- **A non-author human completing the one-hour path.** Not done. The 60-minute timing is a machine
  floor plus reasoning, not a rehearsal. Until someone who did not write this sits it end to end,
  **the one-hour agenda is credible, not proven.** This is the single blocking item.
- **Any live hosted call.** The recovery lane is configuration-only and its lane resolution is
  unit-tested, but nothing has been sent to a hosted endpoint. Schema and verifier semantics are
  unchanged by construction, **not by observation.**
- **Entra / `AzureCliCredential` authentication is not implemented.** It would need `Azure.Identity`,
  which is not in the attendee prefetch and is not referenced by this repo. The `MAF_AUTH=azure-cli`
  flag that used to appear in `.env.example` did nothing and has been removed rather than left to be
  discovered on the day. Use a key against the OpenAI-compatible endpoint.
- **LM Studio parity beyond one machine.** LM Studio now passes the same gates on an M4 Max with
  six models ([LM-STUDIO-SWEEP.md](LM-STUDIO-SWEEP.md)), so it is no longer untested — but that is
  one machine, and the sweep ran at a 262144-token context rather than the intended one. Ollama
  stays the blessed runtime.
- **Free cloud lane.** Not exercised by this application at all. Hosted free routes are throttled,
  mutable and possibly logged; fictional data only, never a workshop dependency.
- **Native Windows Ollama.** Still not run. The 24 Aug CPU-only verification confirmed no native-Windows
  Ollama install exists on the reference machine, so every measurement labelled "Windows" in this repo
  is WSL2. macOS arm64 is measured.
- **Any hardware other than the reference machine**, and any claim about its GPU beyond the measured
  placement percentage.
- **Qwen3.5 4B under this architecture.** Prior work found it passed typed extraction but dropped a
  required fact from prose. It has not been run through this deterministic-rendering pipeline. It is
  pulled only when `WORKSHOP_PREFETCH_QWEN=1`.
- **Tier B offline proof** — physically disconnecting Wi-Fi. `offline-proof.sh` blocks non-loopback
  egress at the firewall for the workshop user, which is strong but not the same as no radio. A
  human should still do the physical test once.
- **Cold-start timing on attendee machines.**
- **Sustained multi-attendee load** on shared venue Wi-Fi during the prefetch step.

## Design choices worth stating plainly

**Who writes the ledger.** The model proposes each claim's kind, value and quote. Ordinary code
attaches the `sourceId`, normalizes kind spellings, merges duplicates across sources, assigns claim
ids, orders everything deterministically and serializes the file. Saying "the model wrote the
ledger" overstates its role; saying "code wrote it" understates it. Both halves are named in the
README table for exactly this reason.

**Step order is a deterministic C# pipeline around MAF agents and tools.** It is not the MAF
Workflows API, and that package is deliberately not referenced. Plain code the attendee can step
through is the point.

**The model reads two of the four evidence files.** `status.txt` and `customer-email.txt` are prose,
which is a model's job. `events.csv` is structured data parsed exactly by code, and `runbook.md` is
policy. Sending all four projected to ≈ 39.8 s for a worse timeline than a CSV parser produces for
free. This is the workshop's thesis applied to itself: narrow jobs.

**`sourceId` is attached by code, not emitted by the model.** Extraction runs per source, so the
application already knows which file a claim came from. `R1-SOURCE-WHITELIST` therefore cannot fire
from ordinary model drift in the current pipeline — it guards the seeded phantom-source defect,
hand-edited ledgers, and any future multi-source extraction. Say this out loud rather than implying
the model is being caught by it.

**Semantic checks are token comparisons, not judgement.** `R12-KIND-SEMANTICS` matches a fixed list
of causal markers ("caused by", "due to", "because", …) as whole-token spans against the claim value
*and* its quotes, so a cause relabelled as an event is still caught. `R11-EVENT-SUPPORTED` requires
an event claim to be a whole-token span of an event description parsed from `events.csv`; anything
else is UNVERIFIED — reported, never asserted. Both are ordinary code with unit tests, including
tests that the runbook's phrase "a confirmed cause" does *not* fire the rule.

**`R2-QUOTE-PRESENT` is scoped to one line.** Normalizing the whole file first let a quote splice
two unrelated lines together and still "occur" in the source. The extraction prompt already required
one-line quotes; the rule now enforces what the prompt asks for.

**"Proves its work" means the declared checks.** Evidence IDs, exact quotes, numbers, timestamps,
durations, required-claim presence, event support, kind honesty, and refusing to assert causation.
It does **not** mean proof of every semantic claim, and it does not mean the brief is true. A claim
can pass every rule and still be a bad summary.

**We never ask a model to grade its own factual correctness.** All verification is ordinary code
comparing against text or against facts parsed independently from source. **There is no LLM judge
anywhere in this repo.**

**Typed output is not truth.** JSON grammar proves shape. That is all it proves.

## Prior work

The earlier readiness pack (`~/work/jackdaw-maf-offline-readiness`, 13 August 2026) measured a
*different* probe — a tool loop with a model-written brief. Its numbers, such as a 5.68 s tool-loop
median, describe that probe and **not** this application. Do not mix the two sets of figures.

Latency figures measured **before** the semantic rules and the sharpened extraction prompt (median
24.5 s / worst 25.1 s, and an independent reproduction at median 24.3 s / worst 24.6 s) describe the
earlier build of *this* application. The current build measures 19–22 s. Do not mix those either.
