# Agenda — 85 minutes (6:00–7:25 PM)

The delivery plan. Cut from AGENDA-90 after the 30 Aug coherence review and attendee simulation:
attendees can finish both TODOs in 45–55 minutes only when nothing goes wrong, so the presenter
types TODO 4 live and the room chooses to write or paste TODO 5. Recovery is a lane from minute 7,
not a segment. Slack is real: 5 minutes at the end, and Gather is 6 minutes shorter than planned.

Six teaching sections, in deck order: Local models → Get started → Talking to a model →
The dataset and Gather → Extract and Analyse → Workflows and graceful failure.

| Min | Segment | Mode | Result on screen |
|---|---|---|---|
| 0–4 | Welcome · build along or observe · the core sequence · agenda | talk | slides 1–4 |
| 4–7 | **Local models** · LM Studio and Ollama, both env triples (LM Studio :1234, Ollama :11434) · why a 4B | talk | slides 5–8 |
| 7–14 | **Get started** · clone, build, `user-secrets`, CP-01 `smoke`. Five-minute rule: anyone without `WORKSHOP_OK` by minute 13 pairs up or reads `workshop/reference-run/` | attendees run | slides 9–13 · `reply: WORKSHOP_OK` |
| 14–18 | **Talking to a model** · chat, tool call, structured output · CP-02 `typed` · reasoning effort | attendees run | slides 14–18 · raw JSON then parsed greeting |
| 18–26 | **The dataset and Gather** · CP-03 `gather --term intersection`, then `--term cyclist` → empty pack. No model involved | attendees run | slides 19–21 · 4 records · `isEmpty: true` |
| 26–44 | **Extract and Analyse** · CP-04 — **presenter types TODO 4 live** (`Agent(...)`, `RunAsync<CrashSelection>`, serialize the pack, catch malformed → null). Attendees type along or paste from `solution/`. Run: gate moves `UnsupportedSelection` → `UnsupportedAnalysis` | build-along | slides 22–24 · extract JSON with 4 IDs |
| 44–54 | **Extract and Analyse** · CP-05 — attendees write TODO 5 (the marker lists finding, actions, questions, **0–100 confidence**) or paste. Run: `gate: Supported`, exit 0. Watch for `confidence: 0` — the instruction forgot to ask | attendees build | slides 25–26 · analyse JSON, ~5 s |
| 54–64 | **Workflows and graceful failure** · live gates you can actually show: the starter stub (`UnsupportedSelection`), `cyclist` (`NoEvidence`); invalid / duplicate / low-confidence via `CrashWorkflowTests.cs` on screen. Then CP-06 `workflow --term intersection` and `ready` | presenter | slides 27–29 · same output, workflow-labelled; `READY:` line |
| 64–72 | One extra (`h` to reveal): `#extra-coding` (three local coding models did both TODOs; Gemma 4 26B compiled with tests green and gated `LowConfidence` forever — one missing sentence) or `#extra-models` (seven models, same app) | talk | hidden slide |
| 72–80 | Key takeaways · thank-you · QR · questions | talk | slides 30–31 |
| 80–85 | slack | — | — |

31 slides in the flow; 5 hidden extras behind `h` (36 total).

## Notes the runbook does not repeat

- Nemotron returns `Supported` at confidence 85+ even for off-topic questions; do not promise a live
  `LowConfidence`. The tests are the demonstration.
- `dotnet test` is green before any TODO is written; "done" is `run --term intersection` exit 0.
- Recovery lane (any minute): `workshop/reference-run/*.txt` has every command's real output.
