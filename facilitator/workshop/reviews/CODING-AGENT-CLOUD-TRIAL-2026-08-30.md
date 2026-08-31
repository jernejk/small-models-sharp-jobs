# Free-tier cloud coding agents vs the offline workshop

**Question:** without a paid coding subscription, can an attendee's agent finish the workshop's two TODOs?
**Answer:** yes. The free path is not a consolation prize — the two free lanes beat the cheapest paid one,
and one of them needs no signup at all.

Run 2026-08-30 (AEST) · OpenCode 1.18.4 · codex-cli 0.149.1 · .NET 10.0.302 · LM Studio at
`http://localhost:1234/v1` serving `nvidia-nemotron-3-nano-4b` (the only model loaded) as the app runtime
throughout. Per-lane `VERIFY.txt`, raw harness logs, `results.tsv` and a timestamped `JOURNAL.md` sit beside
this file. This repeats the 2026-08-30 local-model trial (`../opencode-trial-2026-08-30/`) with cloud models
on the same two checkpoints and the same grader.

---

## 1. The grader, and why `dotnet test` still isn't it

Established on a pristine copy **before any model ran** (`baseline/VERIFY.txt`):

| check | pristine starter, zero work done |
|---|---|
| `dotnet build` | rc=0 |
| `dotnet test` | **green** — Core 9/9, LocalModel 22 passed + 5 skipped |
| `WORKSHOP_LOCAL_MODEL=1 dotnet test` | **rc=1** — LocalModel 26/27, `SupportedTermSelectsRecordsAndProducesAFinding` FAIL |
| `run --term intersection` | **rc=2**, `gate: UnsupportedSelection` |
| diff vs `solution/CrashPipeline.cs` | 23 lines |

The repo cleanup since the last trial shrank Core to 9 tests and made `starter/` standalone, but **the green-test
trap survived it**: an untouched starter passes `dotnet test`. The starter README now warns about this in prose,
which is an improvement — and every one of the four models still fell for it anyway (§4).

**Verdict rule used below:** PASS requires `MODELTEST_RC=0` **and** `run --term intersection` rc=0 with
`gate: Supported`. Every verdict is that pair, run by me after the model exited — never the model's own claim.
The rule demonstrably discriminates: it fails on the pristine baseline and it failed one of the four lanes.

## 2. Matrix

| Harness + model | free? | CP-04 Extract | CP-05 Analyse | End-to-end | diff vs solution |
|---|---|---|---|---|---|
| OpenCode + `opencode/big-pickle` | **free, no signup** | **PASS** 31 s | **PASS** 21 s | **PASS** — 27/27, `Supported` | 39 |
| OpenCode + `openrouterfree/nvidia/nemotron-3-ultra-550b-a55b:free` | free, needs an account | **PASS** 85 s* | **PASS** 44 s | **PASS** — 27/27, `Supported` | **34** |
| Codex CLI + `gpt-5.6-luna` | paid | **PASS** 86 s | **PASS** 78 s | **PASS** — 27/27, `Supported` | 41 |
| Codex CLI + `gpt-5.4-mini` (Pebble) | paid | **FAIL** 66 s | **FAIL** 59 s | **FAIL** — 26/27, `LowConfidence` | 52 |
| OpenCode + `openrouterfree/qwen/qwen3-coder:free` | — | **SKIP** | — | — | — |

\* second attempt; the first died at 18 s on an upstream capacity error (§5).

End-to-end acceptance detail:

| Harness + model | build | offline test | `WORKSHOP_LOCAL_MODEL=1` | `run --term intersection` | verdict |
|---|---|---|---|---|---|
| big-pickle | 0 | green | **27/27** (13 s) | rc=0 `Supported` | ship-ready |
| nemotron-3-ultra:free | 0 | green | **27/27** (12 s) | rc=0 `Supported` | ship-ready |
| gpt-5.6-luna | 0 | green | **27/27** (11 s) | rc=0 `Supported` | ship-ready |
| gpt-5.4-mini | 0 | green | **26/27** (1 FAIL) | rc=2 `LowConfidence` | not usable as-is |

## 3. What each lane produced

### `opencode/big-pickle` — fastest, and the only zero-setup lane
52 s for both checkpoints, the fastest run in either trial. It explored before editing (read `CrashPipeline.cs`,
globbed `Workshop.Core/**/*.cs`, grepped for `ValidateSelection|CrashGate|WorkshopJson`, then read
`CrashWorkflow.cs`, `IncidentDataset.cs`, `WorkshopJson.cs`) and made exactly one edit per checkpoint. Both
instruction strings name the contract fields explicitly, including `Confidence (0-100)` — which is the whole
ballgame (§4). 39-line diff. One cosmetic miss: it left the stale `// TODO 4:` comment above its own
implementation, and its CP-05 summary reported "31 passed" by summing two test assemblies.

### `openrouterfree/nvidia/nemotron-3-ultra-550b-a55b:free` — cleanest code of the trial
**34-line diff, the smallest of any model across both trials.** It wrote the most explicit instruction strings
of the four — a field-by-field list per contract:

```csharp
Return a CrashSelection with:
- RecordIds: array of IDs copied exactly from the evidence pack (no invented IDs)
- Rationale: brief explanation of why these records were selected
- Confidence: integer 0-100 indicating confidence in the selection
```

That is arguably better prompt-writing than the reference solution. Cost: one upstream refusal and a 60 s backoff.

### `gpt-5.6-luna` — correct, unremarkable, 3x the price of free
Both checkpoints correct on the first attempt, 164 s total, ~67k tokens. Reasoning effort `high` per the session
header. 41-line diff — verbose `$"""` prompt blocks where the reference uses a one-line interpolation. Also left
the stale `// TODO 5:` comment. Nothing it did justifies choosing it over either free lane for this task.

### `gpt-5.4-mini` (Pebble) — the instructive failure, twice over
Structurally flawless C#: right helper, right generic, right try/catch, only `CrashPipeline.cs` touched, builds
clean, all offline tests green. And the pipeline never returns a finding. Its `ExtractAgent` instruction reads:

```csharp
Select only the relevant records for the attendee question.
Use only record IDs copied exactly from the supplied evidence pack.
Invent nothing, use no tools, and return only the typed contract.
```

Nothing asks the runtime model to *rate confidence 0-100*. `CrashSelection.Confidence` comes back `0` every run,
`ValidateSelection` returns `LowConfidence`, and code stops before Analyse. The record IDs (all four, correct) and
the rationale are good; only the confidence field was never elicited.

This is the same failure `google/gemma-4-26b-a4b-qat` produced in the previous trial — but Pebble is worse. I
spliced the reference `ExtractAsync` into a copy and kept Pebble's `AnalyseAsync` (`codex-pebble/tree-isolated/`):
extract confidence rose to 95, and the **analysis** still came back `confidence: 0`, `gate: LowConfidence`,
reproduced 3/3 runs. **Pebble omitted the 0-100 ask in both instruction strings.** Unlike Gemma, it has no half
that works.

## 4. Findings

**The free lanes won on merit, not on price.** Ranked by diff-to-reference: OpenRouter free (34) < big-pickle (39)
< Luna (41) < Pebble (52). Ranked by wall time: big-pickle (52 s) < Pebble (125 s, wrong) < OpenRouter (129 s)
< Luna (164 s). The cheapest paid model is the only one that failed.

**"Free" has two very different meanings, and the distinction is load-bearing for the slide.** I ran both free
lanes again against a clean `XDG_DATA_HOME` and a minimal config, with no credentials of any kind:

- `opencode/big-pickle` answered normally and **wrote no `auth.json`** — no account, no API key, no signup.
- `openrouterfree/...:free` failed (`UnknownError`, ref `err_b960a252`). OpenRouter's free tier is free *after*
  you create an account and paste a key.

Only big-pickle satisfies a literal reading of "no subscription needed".

**Every model verified with the tool that cannot fail, and every model was believed by no one but itself.** All
four ran `dotnet build` and `dotnet test` and stopped there. None ran `WORKSHOP_LOCAL_MODEL=1`, none ran
`run --term intersection`, none read the README paragraph that says so. Pebble reported "dotnet test: passed" as
its evidence of success while shipping a pipeline that returns no finding. Two trials, seven models, zero that
found the real gate unprompted — this is a property of the exercise, not of any one model.

**Wall-clock timeout only — confirmed again.** No stall detection was used this time, per the previous trial's
finding that OpenCode buffers stdout until exit. Nothing was killed and no run approached the 900 s cap; the
slowest single checkpoint was 86 s. Cloud latency is roughly 5-10x better than the local 27B models (which took
180-660 s per checkpoint).

## 5. Rate limits and availability incidents

No HTTP 429 occurred anywhere in the trial. Two availability problems did:

1. **`openrouterfree/qwen/qwen3-coder:free` is no longer free.** It is still listed by `opencode models`, but the
   run failed in 3 s with: *"This model is unavailable for free. The paid version is available now — use this slug
   instead: `qwen/qwen3-coder`"*. A stale free slug that still appears in the model list is exactly the kind of
   thing that will embarrass a live demo.
2. **`nvidia/nemotron-3-ultra-550b-a55b:free` hit an upstream capacity refusal** — *"Upstream error from Nvidia:
   Service temporarily overloaded"* — 18 s in, after two file reads and no edits. One 60 s backoff cleared it;
   the retry succeeded in 85 s. Documented free-tier limits (~20 req/min, 50/day) were never approached; this was
   provider capacity, not quota.

Cost: the two free lanes cost nothing. Luna spent ~67k tokens, Pebble ~92k, both against JK's paid Codex
allowance — and Pebble's spend bought a broken pipeline.

## 6. Recommendation for the "no subscription" slide

**Show OpenCode + `opencode/big-pickle`.** It is the only combination in this trial that is free in the sense an
attendee means it: no account, no API key, no card, nothing in `auth.json` — verified by running it against empty
credentials. It also happens to be the fastest thing tested in either trial, finishing both checkpoints in 52
seconds with a 39-line diff, which makes it safe to run live rather than pre-recorded. Keep
`openrouterfree/nvidia/nemotron-3-ultra-550b-a55b:free` as the named backup and say what it costs: an OpenRouter
account, and the acceptance that a free upstream can refuse you mid-run — mine did, and one 60-second retry fixed
it. Do not promise `qwen/qwen3-coder:free`; it is still in the model list and is no longer free.

Two things to warn about out loud. First, **do not tell attendees the free tier is the weak option** — in this
trial the two free lanes produced tighter code than paid Luna, and the cheapest paid model (`gpt-5.4-mini`) was
the only outright failure. Price predicted nothing here. Second, and this is the slide that earns its place:
**Pebble wrote perfect C# that compiled, passed every offline test, and produced a pipeline that never returns a
finding** — because both of its instruction strings forgot to ask the runtime model for a confidence score. It
then reported "dotnet test: passed" as proof of success. That is the workshop's thesis demonstrated by a coding
agent failing at it: small models need sharp, complete jobs, and you validate in code, because the green test was
green before anyone wrote a line. Every model in this trial verified with the check that cannot fail. Only the
model-backed gate told them apart.

## 7. Reproducibility caveats

**The starter's `// TODO 5:` comment changed mid-trial.** A concurrent agent edited canonical `src/` while this
ran. My copies (taken 03:30 local) carried `// TODO 5: Ask the tool-free AnalyseAgent for a grounded typed
finding.`; the repo now reads `...for a typed finding, actions, open questions and 0-100 confidence.` `TODO 4` is
unchanged. The prompts I fed the models came from `prompts/cp-0{4,5}.txt` and were identical for all four lanes,
but the agents that read the source file saw the older comment. **A re-run against the new TODO 5 text should be
expected to reduce the confidence-omission failure rate**, and `workshop/CHECKPOINTS.md` has separately gained a
line telling attendees what `confidence: 0` means. Both changes attack precisely the failure Pebble and Gemma
exhibited — so treat §3's Pebble result as evidence that the fix was needed, not as a prediction of what the
current starter will produce.

**Scope.** CP-01/02/03/06 ship complete in the starter and were not coding tasks for any model; they were
verified by running them (all four lanes: `smoke` rc=0, `typed` rc=0, `gather` 4 records / empty, and `workflow`
matching each lane's own `run` gate). Only CP-04 and CP-05 discriminate.

**Not modified:** the workshop repo. Every trial write landed under this directory; `starter/CrashPipeline.cs`
still has its two TODOs and two `return null;` bodies. No credentials appear in any file here.
