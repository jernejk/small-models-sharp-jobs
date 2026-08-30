# Reasoning effort on the Extract task — 30 Aug 2026

# Reasoning effort vs tokens vs accuracy — Nemotron 3 Nano 4B, Extract task

Extract exactly as `CrashPipeline.ExtractAsync` (same instructions, temperature 0, `RunAsync<CrashSelection>`), `MaxOutputTokens` raised to 4000. Terms `intersection` / `rear` / `overturned` via `IncidentDataset.Gather(max 8)` → 4 / 5 / 3 records; expected ID set = every gathered record, since all of them match the term. 3 repeats per (level, term); 36 calls total, 29 Aug dataset, 30 Aug 2026, M4 Max + LM Studio.

| ReasoningEffort | wire `reasoning_effort` | completion tokens (mean, min–max) | reasoning tokens | wall s (mean) | ID recall | precision | confidence | parse failures |
|---|---|---|---|---|---|---|---|---|
| None | `none` | **115** (93–130) | 0 | **1.4** | 100% | 100% | 95–98 | 0/9 |
| Low | `low` | **434** (366–478) | 225–365 | 4.9 | 100% | 100% | 95 | 0/9 |
| Medium | `medium` | **434** (366–478) | 225–365 | 5.0 | 100% | 100% | 95 | 0/9 |
| High | `high` | **434** (366–478) | 225–365 | 5.0 | 100% | 100% | 95 | 0/9 |

`reasoning_effort` **does** reach LM Studio (captured on the wire), and it is a real on/off switch, not a dial: `none` renders the assistant turn as a closed `<think></think>`, every other value as an open `<think>`, so Low, Medium and High produce byte-identical output and identical token counts. `ReasoningOutput.None` does not suppress the thinking — `reasoning_content` still comes back, it is just dropped before `AgentResponse.Text`. `AgentResponse.Usage` also does not surface `completion_tokens_details.reasoning_tokens`; that column came from the proxy capture.

**Takeaway: on this Extract job reasoning buys nothing — turning it on costs 3.8× the completion tokens and 3.5× the wall clock for exactly the same record IDs, and Low/Medium/High are indistinguishable, so the only meaningful choice is on or off, and off is the right one.**

Files: `results.csv` (36 rows), `chart.png`, `wire.jsonl` (raw request/response per call), `Program.cs` (harness), `proxy.py` (logging proxy on :1235 → :1234), `chart.py`, `lms-log.txt`.

Raw: `reasoning-effort-2026-08-30.results.csv` (36 calls; reasoning_tokens from a wire capture, MAF's Usage does not expose it). Wire finding: MAF forwards `reasoning_effort` correctly; LM Studio's Nemotron template renders `none` as a closed `<think></think>` and every other value as an open `<think>`, so low/medium/high are identical. `ReasoningOutput.None` drops `reasoning_content` from the text but you still pay the tokens.
