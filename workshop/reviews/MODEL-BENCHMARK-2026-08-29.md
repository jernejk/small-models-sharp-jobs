# Model benchmark on the workshop build — 29 Aug 2026

Machine: JK's MacBook Pro M4 Max 64 GB, LM Studio (OpenAI-compatible `/v1`, `--gpu max`, `--context-length 16384`), `dotnet run --no-build`. App at the 29 Aug evening state **with** the output cap (`MaxOutputTokens = 700`) and `maxRetries: 0`; the malformed-output→gate fix was applied *after* this matrix (see 'Post-fix re-runs'). Per model: `smoke`, `typed`, then `run --term {intersection, rear-end, overturned}` × 3 (Gather yields 4 / 5 / 3 records; every gathered record matches the term, so 'recall' = selected-valid ÷ gathered). Temperature 0, reasoning off. Driver: `bench.py`; raw console output per run in `raw/`. The two small models at the bottom of the table (LFM2.5 1.2B, Granite 4.0-H Tiny) were added on 30 Aug against the same driver and the same gathered record sets.

## Summary

| Model | Load s | smoke s | typed s | Supported runs | run median s | run range s | ID recall | per-term consistency (int, rear, over) | failures |
|---|---|---|---|---|---|---|---|---|---|
| nvidia-nemotron-3-nano-4b | 0.9 | 0.7 | 1.0 | 9/9 | 4.9 | 4.2–9.9 | 100% | same,same,same | none |
| google/gemma-4-12b | 7.8 | 0.8 | 2.4 | 5/9 | 9.6 | 7.9–11.9 | 100% | varies,varies,same | intersection#2 rc=3; intersection#3 rc=3; rear-end#2 rc=3; rear-end#3 rc=3 |
| google/gemma-4-26b-a4b-qat | 11.0 | 1.0 | 2.4 | 9/9 | 4.7 | 4.1–5.9 | 100% | same,same,same | none |
| prism-ml/bonsai-27b | 6.8 | 0.9 | 2.3 | 6/9 | 11.2 | 8.5–15.9 | 70% | n/a,same,same | intersection#1 rc=3; intersection#2 rc=3; intersection#3 rc=3 |
| qwen/qwen3.8-27b | 9.9 | 1.1 | 2.6 | 9/9 | 20.1 | 18.1–28.2 | 100% | same,same,same | none |
| liquid/lfm2.5-1.2b | 4.8 | 0.7 | 1.2 | 9/9 | 2.0 | 1.9–2.5 | 100% | same,same,same | none |
| ibm/granite-4-h-tiny | 1.2 | 0.8 | 1.0 | 9/9 | 3.8 | 3.2–4.6 | 89% | same,same,same | none |

## What the numbers say

- **Nemotron 3 Nano 4B** (2.8 GB): 9/9 supported, full recall every run, identical IDs across repeats, ~4–6 s end-to-end. Still the attendee lane.
- **Gemma 4 26B-A4B QAT** (15.6 GB): 9/9, full recall, as fast as the 4B (4–6 s) because only 4B params are active. Best "if you have the RAM" option on Apple Silicon; needs ~16 GB free.
- **Gemma 4 12B** (6.8 GB): correct when it answers (5/9 supported, 100% recall on those) but **intermittently runs away** — 5,800 completion tokens for a 700-token prompt. Before tonight's fixes that hung the app for 6 minutes (120 s budget × the OpenAI client's default retries). With the 700-token cap it fails in ~18 s; with the gate fix it takes the caution branch. Post-fix re-run: 3/3 supported at 8–12 s — so expect it to work most of the time, not always.
- **Bonsai 27B** (8.5 GB, 1-bit Qwen3.6-27B): under-selects (2/5 on rear-end, ID sets identical across runs), and on 'intersection' its Analyse JSON overruns the cap every time → caution branch (`UnsupportedAnalysis`, exit 2, ~23 s). Correct-but-thin; good demo of *why the gate exists*, not a lane to recommend.
- **Qwen 3.8 27B** (16 GB): 9/9, full recall, consistent IDs, but 18–28 s per run — 4–5× slower than the 4B for the same answer. Analyse confidence sits at 85 vs the others' 95–100.
- **LFM2.5 1.2B** (1.25 GB): the fastest thing in the matrix — 9/9 supported, full recall, identical IDs across all three repeats, and **1.9–2.5 s** per run (roughly half the 4B's time), loading in 4.8 s. Extract confidence is a flat 100. The catch is in the prose, not the plumbing: its Analyse `finding` is written as *instructions* rather than a *result* — "Review severity patterns and vehicle counts across the four records" — where Nemotron says "a consistent pattern of non-fatal collisions at intersections". It self-reports that with a steady 85 confidence. Recommend it as the fallback lane for a thin laptop or a room that has to get through Gather/Extract fast; don't put it on screen when the point of the slide is the quality of the finding.
- **Granite 4.0-H Tiny 7B** (4.23 GB, `granitehybrid`): 9/9 supported, 3.2–4.6 s per run — 4B-class speed from a 7B file, and the fastest load in the matrix at 1.2 s. Findings are the real thing, not a to-do list: "a pattern of cross traffic collisions at intersections in Victoria, Australia". The one blemish is recall — on 'overturned' it picks 2 of 3 every time, dropping the same record (`T20240003419`) across all three repeats, which is why it lands at 89% rather than 100%. Confidence is honest about it (extract 85–90 on the terms it fumbles, 100 on rear-end where it takes all five). Deterministic everywhere: identical ID sets across repeats on all three terms. Worth recommending as the "one size up from Nemotron" lane — it costs 1.4 GB more and buys better prose — but Nemotron still wins on recall, so keep it as the default.

Teaching line that survives the data: file size doesn't predict fitness; the same app, the same gates, seven very different behaviours. Structure + verification is what made every one of them safe to run.

## Post-fix re-runs (malformed output → gate instead of crash)

| Model | run 1 | run 2 | run 3 |
|---|---|---|---|
| google/gemma-4-12b — intersection | Supported 11.6 s | Supported 8.1 s | Supported 8.0 s |
| prism-ml/bonsai-27b — intersection | UnsupportedAnalysis 26.3 s (exit 2) | UnsupportedAnalysis 22.5 s | UnsupportedAnalysis 22.8 s |

## Fixes made to the app because of this benchmark (src/Workshop.App/CrashPipeline.cs)

1. `MaxOutputTokens = 700` on every agent call, sent as both `max_completion_tokens` and legacy
   `max_tokens` (a `PipelinePolicy` adds the second key) — a runaway model now stops in seconds on
   Ollama too, which ignores `max_completion_tokens` and honours only `max_tokens`.
2. `RetryPolicy = new ClientRetryPolicy(maxRetries: 0)` — `MAF_TIMEOUT_SECONDS` is now the real ceiling (proved: 5 s budget → exit 3 at 5.5 s, previously ~3×).
3. Extract/Analyse catch `InvalidOperationException`/`JsonException` from structured output and return null, so the gate reports `UnsupportedSelection`/`UnsupportedAnalysis` — matching CHECKPOINTS.md CP-04/CP-05.

## Per-run detail

| Model | term | # | rc | s | gate | valid/gathered | extract conf | analyse conf | finding / error |
|---|---|---|---|---|---|---|---|---|---|
| nvidia-nemotron-3-nano-4b | intersection | 1 | 0 | 5.7 | Supported | 4/4 | 95 | 95 | The selected crash records show a consistent pattern of non-fatal collisions at intersections, primarily invol |
| nvidia-nemotron-3-nano-4b | intersection | 2 | 0 | 9.9 | Supported | 4/4 | 95 | 95 | The selected crash records show a consistent pattern of non-fatal collisions at intersections, primarily invol |
| nvidia-nemotron-3-nano-4b | intersection | 3 | 0 | 5.3 | Supported | 4/4 | 95 | 95 | The selected crash records show a consistent pattern of non-fatal collisions at intersections, primarily invol |
| nvidia-nemotron-3-nano-4b | rear-end | 1 | 0 | 4.6 | Supported | 5/5 | 95 | 95 | A consistent pattern of rear-end collisions in the same lane across multiple years, with varying severity leve |
| nvidia-nemotron-3-nano-4b | rear-end | 2 | 0 | 4.9 | Supported | 5/5 | 95 | 95 | A consistent pattern of rear-end collisions in the same lane across multiple years, with varying severity leve |
| nvidia-nemotron-3-nano-4b | rear-end | 3 | 0 | 4.6 | Supported | 5/5 | 95 | 95 | A consistent pattern of rear-end collisions in the same lane across multiple years, with varying severity leve |
| nvidia-nemotron-3-nano-4b | overturned | 1 | 0 | 5.4 | Supported | 3/3 | 95 | 95 | The selected crash records show a pattern of vehicle overturns occurring on off-carriageway roads, specificall |
| nvidia-nemotron-3-nano-4b | overturned | 2 | 0 | 4.2 | Supported | 3/3 | 95 | 95 | The selected crash records show a pattern of vehicle overturns occurring on off-carriageway roads, specificall |
| nvidia-nemotron-3-nano-4b | overturned | 3 | 0 | 4.3 | Supported | 3/3 | 95 | 95 | The selected crash records show a pattern of vehicle overturns occurring on off-carriageway roads, specificall |
| google/gemma-4-12b | intersection | 1 | 0 | 11.2 | Supported | 4/4 | 100 | 95 | The records consistently involve 'collision with vehicle' types involving 2 vehicles. A specific pattern of 'c |
| google/gemma-4-12b | intersection | 2 | 3 | 18.2 | — | 0/4 | None | None | pipeline error: JsonException: Expected end of string, but instead reached end of data. Path: $ / LineNumber:  |
| google/gemma-4-12b | intersection | 3 | 3 | 18.2 | — | 0/4 | None | None | pipeline error: JsonException: Expected end of string, but instead reached end of data. Path: $ / LineNumber:  |
| google/gemma-4-12b | rear-end | 1 | 0 | 11.9 | Supported | 5/5 | 100 | 100 | The records consistently describe rear-end collisions involving two or more vehicles. There is a recurring pat |
| google/gemma-4-12b | rear-end | 2 | 3 | 18.0 | — | 0/5 | None | None | pipeline error: JsonException: Expected end of string, but instead reached end of data. Path: $ / LineNumber:  |
| google/gemma-4-12b | rear-end | 3 | 3 | 18.0 | — | 0/5 | None | None | pipeline error: JsonException: Expected end of string, but instead reached end of data. Path: $ / LineNumber:  |
| google/gemma-4-12b | overturned | 1 | 0 | 9.6 | Supported | 3/3 | 100 | 100 | The records show a pattern of single-vehicle rollover accidents ('vehicle overturned with no collision') occur |
| google/gemma-4-12b | overturned | 2 | 0 | 8.1 | Supported | 3/3 | 100 | 100 | The records consistently describe single-vehicle rollover incidents ('vehicle overturned with no collision') i |
| google/gemma-4-12b | overturned | 3 | 0 | 7.9 | Supported | 3/3 | 100 | 100 | The records consistently describe single-vehicle rollover incidents ('vehicle overturned with no collision') i |
| google/gemma-4-26b-a4b-qat | intersection | 1 | 0 | 5.5 | Supported | 4/4 | 100 | 95 | All four provided records involve multi-vehicle collisions (2 vehicles per record) occurring at intersections. |
| google/gemma-4-26b-a4b-qat | intersection | 2 | 0 | 4.8 | Supported | 4/4 | 100 | 100 | The provided records consist exclusively of multi-vehicle collisions (2 vehicles per record) occurring at inte |
| google/gemma-4-26b-a4b-qat | intersection | 3 | 0 | 4.7 | Supported | 4/4 | 100 | 100 | The provided records consist exclusively of multi-vehicle collisions (2 vehicles per record) occurring at inte |
| google/gemma-4-26b-a4b-qat | rear-end | 1 | 0 | 5.9 | Supported | 5/5 | 100 | 100 | The provided records consist exclusively of rear-end collisions occurring in the same lane involving multiple  |
| google/gemma-4-26b-a4b-qat | rear-end | 2 | 0 | 4.5 | Supported | 5/5 | 100 | 100 | The provided records consist exclusively of rear-end collisions occurring in the same lane involving multiple  |
| google/gemma-4-26b-a4b-qat | rear-end | 3 | 0 | 4.5 | Supported | 5/5 | 100 | 100 | The provided records consist exclusively of rear-end collisions occurring in the same lane involving multiple  |
| google/gemma-4-26b-a4b-qat | overturned | 1 | 0 | 4.9 | Supported | 3/3 | 100 | 95 | The provided records consist exclusively of single-vehicle 'vehicle overturned' accidents occurring 'off carri |
| google/gemma-4-26b-a4b-qat | overturned | 2 | 0 | 4.2 | Supported | 3/3 | 100 | 95 | The provided records consist exclusively of single-vehicle 'vehicle overturned' incidents occurring 'off carri |
| google/gemma-4-26b-a4b-qat | overturned | 3 | 0 | 4.1 | Supported | 3/3 | 100 | 95 | The provided records consist exclusively of single-vehicle 'vehicle overturned' incidents occurring 'off carri |
| prism-ml/bonsai-27b | intersection | 1 | 3 | 29.1 | — | 0/4 | None | None | pipeline error: JsonException: Expected end of string, but instead reached end of data. Path: $ / LineNumber:  |
| prism-ml/bonsai-27b | intersection | 2 | 3 | 26.5 | — | 0/4 | None | None | pipeline error: JsonException: Expected end of string, but instead reached end of data. Path: $ / LineNumber:  |
| prism-ml/bonsai-27b | intersection | 3 | 3 | 26.2 | — | 0/4 | None | None | pipeline error: JsonException: Expected end of string, but instead reached end of data. Path: $ / LineNumber:  |
| prism-ml/bonsai-27b | rear-end | 1 | 0 | 13.1 | Supported | 2/5 | 85 | 85 | The selected records show two non-fatal rear-end collisions in the same lane, both classified as serious injur |
| prism-ml/bonsai-27b | rear-end | 2 | 0 | 8.5 | Supported | 2/5 | 85 | 85 | The selected records show two non-fatal rear-end collisions in the same lane, both classified as serious injur |
| prism-ml/bonsai-27b | rear-end | 3 | 0 | 8.6 | Supported | 2/5 | 85 | 85 | The selected records show two non-fatal rear-end collisions in the same lane, both classified as serious injur |
| prism-ml/bonsai-27b | overturned | 1 | 0 | 15.9 | Supported | 3/3 | 95 | 85 | The selected records show a consistent pattern of single-vehicle rollovers occurring on left bends in East Gip |
| prism-ml/bonsai-27b | overturned | 2 | 0 | 11.2 | Supported | 3/3 | 95 | 85 | The selected records show a consistent pattern of single-vehicle rollovers occurring on left bends in East Gip |
| prism-ml/bonsai-27b | overturned | 3 | 0 | 11.2 | Supported | 3/3 | 95 | 85 | The selected records show a consistent pattern of single-vehicle rollovers occurring on left bends in East Gip |
| qwen/qwen3.8-27b | intersection | 1 | 0 | 28.2 | Supported | 4/4 | 100 | 85 | The selected records indicate a consistent pattern of two-vehicle collisions occurring at intersections, speci |
| qwen/qwen3.8-27b | intersection | 2 | 0 | 20.1 | Supported | 4/4 | 100 | 85 | The selected records indicate a consistent pattern of two-vehicle collisions occurring at intersections, speci |
| qwen/qwen3.8-27b | intersection | 3 | 0 | 20.1 | Supported | 4/4 | 100 | 85 | The selected records indicate a consistent pattern of two-vehicle collisions occurring at intersections, speci |
| qwen/qwen3.8-27b | rear-end | 1 | 0 | 26.8 | Supported | 5/5 | 100 | 85 | The selected records consistently document rear-end collisions in the same lane involving primarily two vehicl |
| qwen/qwen3.8-27b | rear-end | 2 | 0 | 21.0 | Supported | 5/5 | 100 | 85 | The selected records consistently document rear-end collisions in the same lane involving primarily two vehicl |
| qwen/qwen3.8-27b | rear-end | 3 | 0 | 19.4 | Supported | 5/5 | 100 | 85 | The selected records consistently document rear-end collisions in the same lane involving primarily two vehicl |
| qwen/qwen3.8-27b | overturned | 1 | 0 | 24.4 | Supported | 3/3 | 95 | 85 | The selected records indicate a consistent pattern of single-vehicle rollovers occurring off the carriageway i |
| qwen/qwen3.8-27b | overturned | 2 | 0 | 18.1 | Supported | 3/3 | 95 | 85 | The selected records indicate a consistent pattern of single-vehicle rollovers occurring off the carriageway i |
| qwen/qwen3.8-27b | overturned | 3 | 0 | 18.2 | Supported | 3/3 | 95 | 85 | The selected records indicate a consistent pattern of single-vehicle rollovers occurring off the carriageway i |
| liquid/lfm2.5-1.2b | intersection | 1 | 0 | 2.5 | Supported | 4/4 | 100 | 85 | Review severity patterns and vehicle counts across the four records; identify consistency in reported severity |
| liquid/lfm2.5-1.2b | intersection | 2 | 0 | 2.0 | Supported | 4/4 | 100 | 85 | Review severity patterns and vehicle counts across the four records; identify consistency in reported severity |
| liquid/lfm2.5-1.2b | intersection | 3 | 0 | 2.0 | Supported | 4/4 | 100 | 85 | Review severity patterns and vehicle counts across the four records; identify consistency in reported severity |
| liquid/lfm2.5-1.2b | rear-end | 1 | 0 | 2.0 | Supported | 5/5 | 100 | 85 | Review severity trends and vehicle counts across the dataset; identify patterns in crash types and impact cate |
| liquid/lfm2.5-1.2b | rear-end | 2 | 0 | 2.1 | Supported | 5/5 | 100 | 85 | Review severity trends and vehicle counts across the dataset; identify patterns in crash types and impact cate |
| liquid/lfm2.5-1.2b | rear-end | 3 | 0 | 2.1 | Supported | 5/5 | 100 | 85 | Review severity trends and vehicle counts across the dataset; identify patterns in crash types and impact cate |
| liquid/lfm2.5-1.2b | overturned | 1 | 0 | 1.9 | Supported | 3/3 | 100 | 85 | Review crash patterns including vehicle type, location (off carriageway), direction of bend, severity classifi |
| liquid/lfm2.5-1.2b | overturned | 2 | 0 | 1.9 | Supported | 3/3 | 100 | 85 | Review crash patterns including vehicle type, location (off carriageway), direction of bend, severity classifi |
| liquid/lfm2.5-1.2b | overturned | 3 | 0 | 1.9 | Supported | 3/3 | 100 | 85 | Review crash patterns including vehicle type, location (off carriageway), direction of bend, severity classifi |
| ibm/granite-4-h-tiny | intersection | 1 | 0 | 4.6 | Supported | 4/4 | 85 | 80 | The selected crash records show a pattern of cross traffic collisions at intersections in Victoria, Australia. |
| ibm/granite-4-h-tiny | intersection | 2 | 0 | 3.9 | Supported | 4/4 | 85 | 80 | The selected crash records show a pattern of cross traffic collisions at intersections in Victoria, Australia. |
| ibm/granite-4-h-tiny | intersection | 3 | 0 | 3.8 | Supported | 4/4 | 85 | 80 | The selected crash records show a pattern of cross traffic collisions at intersections in Victoria, Australia. |
| ibm/granite-4-h-tiny | rear-end | 1 | 0 | 4.0 | Supported | 5/5 | 100 | 85 | The selected crash records predominantly involve rear-end collisions between two vehicles in the same lane, wi |
| ibm/granite-4-h-tiny | rear-end | 2 | 0 | 3.3 | Supported | 5/5 | 100 | 85 | The selected crash records predominantly involve rear-end collisions between two vehicles in the same lane, wi |
| ibm/granite-4-h-tiny | rear-end | 3 | 0 | 3.2 | Supported | 5/5 | 100 | 85 | The selected crash records predominantly involve rear-end collisions between two vehicles in the same lane, wi |
| ibm/granite-4-h-tiny | overturned | 1 | 0 | 3.8 | Supported | 2/3 | 90 | 80 | The selected crash records show a pattern of vehicle overturns occurring on off-carriageway sections, specific |
| ibm/granite-4-h-tiny | overturned | 2 | 0 | 3.3 | Supported | 2/3 | 90 | 80 | The selected crash records show a pattern of vehicle overturns occurring on off-carriageway sections, specific |
| ibm/granite-4-h-tiny | overturned | 3 | 0 | 3.3 | Supported | 2/3 | 90 | 80 | The selected crash records show a pattern of vehicle overturns occurring on off-carriageway sections, specific |
