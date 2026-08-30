# Saved reference output

Recorded 30 Aug 2026 on the presenter Mac (M4 Max, LM Studio, `nvidia-nemotron-3-nano-4b`, 16k context). Use these in the recovery lane when a machine has no working model: read the file instead of running the command.

| File | Command | Result |
|---|---|---|
| `smoke.txt` | `smoke` | `reply: WORKSHOP_OK` |
| `typed.txt` | `typed` | raw JSON + parsed greeting/confidence |
| `gather-intersection.json` | `gather --term intersection` | 4 records (deterministic, no model) |
| `run-intersection.txt` | `run --term intersection` | Extract 4 IDs → Analyse finding, `gate: Supported` |
| `run-cyclist.txt` | `run --term cyclist` | `gate: NoEvidence` — Extract/Analyse never called |
| `workflow-intersection.txt` | `workflow --term intersection` | same as `run`, workflow-labelled |
| `ready.txt` | `ready` | `READY: model-backed supported path completed.` |

Model output is not deterministic across runtimes; the shape and the gate outcomes are what matter.
