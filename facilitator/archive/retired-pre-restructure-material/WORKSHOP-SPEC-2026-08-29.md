# Small Models, Sharp Jobs — agreed workshop specification

## Intent

Deliver an 85-minute Global AI Construct workshop that teaches a portable offline pattern:
small, named jobs; bounded evidence; typed handoffs; deterministic checks; and sequential
orchestration. Attendees leave with a console program they can reason about, rather than a
long-running autonomous agent they cannot debug.

## Non-goals

- Do not make a Harness, MCP server, vector database, arbitrary filesystem agent, database, or
  cloud account a core dependency.
- Do not claim local runtimes support a combined tool-and-schema-constrained call through the
  current Microsoft Agent Framework path.
- Do not use real incident data until its source and reuse terms are recorded and verified.
- Do not make the model the authority for validation, routing, or completion.

## Learning path and checkpoints

| Stage | What attendees see | Authority | Effort |
| --- | --- | --- | --- |
| 1. Hello | local console prompt returns one reply | model | low |
| 2. Typed answer | no-tool JSON-shaped/typed result | schema + code | low |
| 3. Gather | date-and-term query filters one approved local incident dataset | deterministic C# | low |
| 4. Extract | original question plus compact evidence pack becomes structured relevant records | model output + schema validation | medium |
| 5. Analyse | compact structured records become a grounded finding | model, constrained by evidence | high |
| 6. Branch | no evidence, invalid/low confidence, and supported outcomes visibly diverge | code | deterministic |

Start with explicit calls in `Program.cs`: Gather → Extract → Analyse. Then present a linear
workflow representation of the exact same sequence; it adds reuse and a visible topology, not
new intelligence. Keep it sequential for local hardware.

The current executable `gather` checkpoint implements stage 3 without a model:

```bash
dotnet run --project src/Workshop.App -- gather --term intersection
```

It has a clear empty outcome and caps the evidence pack. The ready Victorian crash sample is the
default workshop dataset; its isolated source assessment under `workshop/data/` keeps the Gather
contract swappable without changing the lesson.

## Architecture and boundaries

```text
user question
  -> deterministic Gather (approved dataset, date + term, capped pack)
  -> ExtractAgent (question + pack -> typed relevant records)
  -> code validation / confidence gate
  -> AnalyseAgent (compact structured evidence -> grounded finding)
  -> deterministic user-facing formatting
```

Gather is intentionally deterministic even when exposed behind a later agent tool. The agent may
ask for a date and term, but it cannot name a path or see the whole folder. Extract does not
self-certify trust: code validates shape, references, counts and thresholds before Analysis. A
deterministic low-confidence or no-evidence message is a successful outcome.

## Compatibility decision

The existing spike evidence establishes that Azure Responses can combine tools and typed output;
the tested local LM Studio and current MAF typed-output path cannot reliably do that combined call.
Therefore the portable core is split gather then typed extraction. A prompt asking for JSON followed
by parse/validate/bounded retry is a possible **bonus workaround**, not schema-constrained output
and not a replacement for validation. Never silently accept malformed JSON.

## Bonus slide: Harness and MCP

MCP/function tools expose capabilities. A Harness packages a model-directed loop around them:
planning, task/session state, compaction, approvals, recovery and stopping. A manually written
tool loop can reproduce those behaviours, but must own them. The workshop uses a workflow because
the safe sequence is known; a Harness becomes relevant when the investigation is genuinely
open-ended. Tool outputs must remain constrained if a controller should not see raw evidence.

## 85-minute timing

| Minutes | Segment |
| --- | --- |
| 0–8 | setup and local hello checkpoint |
| 8–16 | typed no-tool JSON checkpoint and the contract idea |
| 16–30 | Gather: bounded dataset, date/term filtering, evidence pack |
| 30–45 | Extract: question + pack, raw structured JSON, validation gate |
| 45–59 | Analyse: increased effort on reduced evidence; grounded finding |
| 59–70 | explicit calls, then the identical linear workflow and branches |
| 70–80 | no-result, malformed/low-confidence recovery paths |
| 80–85 | recap and optional Harness/MCP comparison |

## Hybrid-room delivery

This is a build-along **and** an observer-friendly demonstration. Every stage has an on-screen
checkpoint and a saved example so local setup is never a prerequisite for understanding the lesson.

| Checkpoint | Builder action | Observer view / recovery |
| --- | --- | --- |
| Hello | run the one-prompt console path | facilitator shows the saved successful console exchange |
| Typed JSON | run the no-tool typed call | show the expected JSON and schema before live output |
| Gather | run `gather --term intersection` | open `reference-run/gather-intersection.json`; use it if a machine is not ready |
| Extract | pass that exact pack to the extractor | show saved raw structured output, then explain code validation |
| Analyse and branch | run the supported, no-result and low-confidence paths | use the saved outcomes and narrate why each branch occurred |

Attendees begin in `starter/`; `solution/` is the explicit catch-up state. At the first failed
local prerequisite, they should switch to observing rather than debugging in public. The recovery
card must make the same call: copy the stage from `solution/`, run the deterministic `gather`
checkpoint, and rejoin at the next visible handoff. Observers still see the full evidence pack,
typed boundary, branch decision and final interpretation.

For the optional provider-neutral recovery lane, see
[ADVANCED-OPENAI-COMPATIBLE-RECOVERY.md](../../workshop/ADVANCED-OPENAI-COMPATIBLE-RECOVERY.md). It is not an
attendee dependency and makes no claim that any free route is already compatible.
