# Saved reference output

Recorded 31 Aug 2026 on the presenter Mac (M4 Max) from `workshop/06-workflow`, against the blessed
attendee default: Ollama `nemotron-3-nano:4b` on `http://localhost:11434/v1`. Use these in the
recovery lane when a machine has no working model: read the file instead of running the command.

Every command below was run with the prompt `"Show up to 5 intersection crashes from 2012."` unless
the table says otherwise.

| File | Command | Result |
|---|---|---|
| `smoke.txt` | `smoke` | `reply: WORKSHOP_OK` |
| `typed.txt` | `typed` | raw JSON + parsed `action`/`target` |
| `query-intersection.txt` | `query --prompt …` | model filter `"intersection crash"` → validated `"intersection"` |
| `gather-intersection.json` | `gather --term intersection` | 8 records (deterministic, no model, default cap) |
| `run-intersection.txt` | `run --prompt …` | 5 gathered → Extract IDs → Analyse finding, `gate: Supported` |
| `run-cyclist.txt` | `run --prompt "Find cyclist crashes."` | `gate: NoEvidence` — Extract/Analyse never called |
| `workflow-intersection.txt` | `workflow --prompt …` | same as `run`, workflow-labelled |
| `ready.txt` | `ready --prompt …` | `READY: model-backed supported path completed.` |

`query-intersection.txt` is the one to show when someone asks who owns the filter: the model asked for
the term `intersection crash`, and C# validation reduced it to `intersection` before Gather ran.

Model output is not deterministic across runtimes; the shape and the gate outcomes are what matter.
The `gather` line is the exception — it calls no model, so its 8 records are reproducible exactly.
