# OpenCode + local models vs the offline workshop

**Question:** can a local coding model, driven by OpenCode, complete the workshop checkpoints on its own?
**Answer:** yes — two of three models finished the whole attendee task unaided and verifiably.

Run 2026-08-30 (AEST) · 64 GB Apple Silicon · OpenCode 1.18.4 · .NET 10.0.302 · LM Studio at
`http://localhost:1234/v1` · app runtime model throughout: `nvidia-nemotron-3-nano-4b`.
Raw logs, per-model `VERIFY.txt`, `results.tsv` and a timestamped `JOURNAL.md` sit beside this file.

---

## 1. Two corrections to the experiment's premise

Both were established with evidence *before* any model ran. They change how the matrix should be read.

**The starter has two implementable TODOs, not four.** `grep -rn TODO starter/src` returns exactly
`TODO 4` (Extract) and `TODO 5` (Analyse), both in `starter/src/Workshop.App/CrashPipeline.cs`.
TODO 2 and 3 live only in canonical `src/Workshop.App/IncidentPipeline.cs`, which is not generated
into `starter/`. CP-01, CP-02, CP-03 and CP-06 ship **already implemented** — they are checkpoints an
attendee *runs*, not code they write. So each model got two coding tasks; the other four checkpoints
were verified by running them, and are identical across all three models.

**`dotnet test` cannot grade this exercise.** The pristine starter already passes **151 tests**
(129 Core + 22 LocalModel, 5 skipped). Both TODOs live in `Workshop.App`, which the offline suite
never exercises. "Tests pass" is true *before any work is done*.

> This is the most workshop-relevant finding in the whole trial. The starter README and
> ATTENDEE-GUIDE both point attendees at `dotnet test` as the loop. An attendee — or a coding agent —
> who takes green tests as "done" will ship two stubs and never know. Every model in this trial ran
> `dotnet test`, saw green, and reported success; only the checks below distinguished them.

The real grader is the model-backed suite behind `WORKSHOP_LOCAL_MODEL=1`
(`tests/Workshop.LocalModel.Tests/LocalModelTests.cs`). Its
`SupportedTermSelectsRecordsAndProducesAFinding` asserts `CrashGate.Supported`, non-empty
`RecordIds`, every ID present in the pack, and a non-empty `Analysis.Finding` — exactly CP-04+CP-05.
**Every verdict below is that suite plus CLI exit codes, run by me, not by the model.**

## 2. Matrix

| Model | CP-01 | CP-02 | CP-03 | CP-04 Extract | CP-05 Analyse | CP-06 Workflow |
|---|---|---|---|---|---|---|
| `prism-ml/bonsai-27b` (1-bit, 8.52 GB) | PASS | PASS | PASS | **PASS** 180 s | **PASS** 180 s | PASS |
| `qwen/qwen3.8-27b` (16.08 GB) | PASS | PASS | PASS | **PASS** 420 s | **PASS** 660 s | PASS |
| `google/gemma-4-26b-a4b-qat` (15.64 GB) | PASS | PASS | PASS | **PARTIAL** 660 s | **PASS (isolated)** 90 s | FAIL |

CP-01/02/03 are model-independent (starter ships them complete); they confirm the runtime, not the
coding model. CP-06 likewise ships complete — Gemma's FAIL there is its own CP-04 gating the
pipeline, not a defect in `CrashWorkflow`.

**End-to-end acceptance** (`WORKSHOP_LOCAL_MODEL=1` + `run`/`workflow`/`ready`):

| Model | build | offline 151 | model-backed 27 | `run` gate | verdict |
|---|---|---|---|---|---|
| bonsai-27b | 0 | pass | **27/27** (11 s) | `Supported` rc=0 | ship-ready |
| qwen3.8-27b | 0 | pass | **27/27** (14 s) | `Supported` rc=0 | ship-ready |
| gemma-4-26b-a4b-qat | 0 | pass | **26/27** (1 FAIL) | `LowConfidence` rc=2 | not usable as-is |

## 3. What each model actually produced

### `prism-ml/bonsai-27b` — fastest, fully correct
180 s per checkpoint, first tool call ~90 s. Explored sensibly (read `CrashPipeline.cs`, globbed
Core, grepped for `record CrashSelection|record EvidencePack|WorkshopJson.Serialize`, read
`CrashWorkflow.cs` and `WorkshopJson.cs`) before editing. Both implementations use the private
`Agent()` helper, `RunAsync<T>`, `WorkshopJson.Serialize`, and the correct try/catch. Analyse
receives only `selected`, never the pack. 61-line diff vs solution — all of it verbose instruction
strings and a `try` block widened to wrap `RunAsync` as well as `.Result`. Behaviourally equivalent.

### `qwen/qwen3.8-27b` — slowest, cleanest code
2.3x bonsai's wall time, almost entirely prompt ingest (first tool call ~5.5 min). Produced the
**closest code to the reference — 39-line diff**, the smallest of the three: raw-string prompt
literals and a `try` around `.Result` only, matching the solution's structure. Correctly left
`CrashWorkflow.cs` untouched and changed only `CrashPipeline.cs`.

### `google/gemma-4-26b-a4b-qat` — right shape, wrong prompt
Structurally correct C#: right helper, right generic, right try/catch, only `CrashPipeline.cs`
touched. It fails on the thing this workshop is actually about. Its instruction strings are one
terse line each:

```csharp
var agent = Agent("ExtractAgent", "Select only relevant records, invent nothing, and use no tools.");
```

Nothing asks the model to *rate confidence*. `CrashSelection.Confidence` therefore comes back `0` on
every run, `ValidateSelection` returns `LowConfidence`, and code stops before Analyse — reproducible
across repeated runs. RecordIds and rationale were both good; only the confidence field was never
elicited. Gemma also emitted `AnalyseAsync` at the wrong indentation (8 spaces in a 4-space file).

To be fair to it I swapped in the reference `ExtractAsync` and kept Gemma's `AnalyseAsync`: **27/27
pass, `gate: Supported`, confidence 100.** Gemma's Analyse code is genuinely correct; a single
under-specified prompt string in its Extract is the whole failure.

## 4. Operational findings

**My stall detector was wrong and cost real time.** OpenCode buffers stdout and flushes only at
exit, so "no log growth for 300 s" fires on healthy runs. It false-positived twice — killing Qwen
CP-05 at 660 s and Gemma CP-04 at 331 s, both of which had *already finished the work*. Qwen's was
caught by re-checking the tree (TODO 5 gone, tests green) instead of trusting the kill; Gemma's cost
a full wasted attempt. **Anyone repeating this should use a plain wall-clock timeout only.** The
terminal-title escape codes OpenCode writes are also not a progress signal.

**LM Studio ignores `--context-length` for these builds.** All three loaded at their own defaults
(bonsai 251648, qwen 208384, gemma 262144) regardless of `--context-length 65536`, and `--parallel 1`
did not change it either. OpenCode's own `limit.context: 65536` still governs the request, so this
cost memory, not correctness. No edit to `opencode.jsonc` was needed — all three were already
configured at 65536/8192.

**Memory: the obvious metric lies here.** `vm.swapusage` *rose* from 7.2 GB to 9.3 GB while I
unloaded 19 GB of models, and sat at 9.3 GB with 25.9 GB of RAM free — macOS never shrinks the swap
file, and uptime was 3d 10h. The honest indicators were `memory_pressure` (49-75% free throughout)
and session pageouts (+~2,900 total). `Pages free` near zero with ~21 GB inactive is normal. No
crash, no wedge, at any point. Judge this box by `memory_pressure`, not `swapusage`.

**Harness trap worth fixing in the repo.** `TestFixtures.EvidenceDir` walks *up* from the test binary
for a directory named `evidence-pack`, which lives at the **repo root**, not inside `starter/`.
Copying `starter/` alone anywhere makes 87 Core tests fail with `DirectoryNotFoundException`. An
attendee who copies just the starter folder — a natural thing to do — hits this and will read it as
their own mistake. Worth either moving `evidence-pack/` into `starter/` or saying so in the guide.

**Invocation.** v1.18.4 takes the message positionally and needs `--auto`, or the run blocks forever
on a permission prompt:
`timeout 900 opencode run -m lmstudio/<model> --auto --dir <copy> "<prompt>"`

## 5. Recommendation for the workshop

The "no cloud subscription" story holds, and it is stronger than expected: **OpenCode driving a
27B-class local model completed the entire attendee task unaided — twice.** Bonsai finished both
checkpoints in 3 minutes each and Qwen wrote the cleanest code of the three, both landing on
`gate: Supported` with all 27 model-backed tests green, on a machine that was also serving the
workshop's own 4B runtime model at the same time. For JK's line on stage: if you don't have a cloud
subscription, OpenCode plus a local model can genuinely do this workshop — say it, and show
bonsai-27b, which is the one to demo (8.5 GB, fastest, correct first time).

Two caveats worth saying out loud rather than hiding. First, budget for latency, not capability:
prompt ingest dominates, and a 16 GB model spent 5.5 minutes before its first tool call on a 64 GB
machine — fine for a build-along, painful for a live demo, so pre-warm the model before you present.
Second, Gemma is the instructive failure and is worth a slide: it wrote structurally perfect C# that
compiled, passed all 151 tests, and still produced a pipeline that never returns a finding — because
one instruction string forgot to ask for a confidence score. That is exactly the workshop's thesis
(small models need sharp, complete jobs; validate in code) demonstrated by a coding model failing at
it. It also shows why the green-tests trap in §1 matters: all three models reported success, and
only running the model-backed gate told them apart.
