# 05 — Analyse

Extract is done and its IDs have already been through the gate. `selected` holds records the code
looked up itself. Now a third agent turns those into a finding — and it is the only call in the
workshop that produces free text a human might act on.

So it gets the smallest possible input. Not the corpus, not the evidence pack: `selected`, and nothing else.

| File | What's in it |
|---|---|
| `Program.cs` | config, the chat client, and the visible flow |
| `GatherAgent.cs` | the filter agent from lab 03 |
| `ExtractAgent.cs` | the selection agent from lab 04 |
| `AnalyseAgent.cs` | **new** — the analysis agent, and the records it is allowed to see |
| `Models.cs` | lab 04's types plus **`CrashAnalysis`** |
| `Gates.cs` | lab 04's gate plus **`ValidateAnalysis`** and `UnsupportedAnalysis` |
| `Utilities.cs` | unchanged from lab 03 |

**Change only:** `Program.cs`, at the CP-05 marker.

## What you're writing

Two lines, inside the `if (gate is CrashGate.Supported)` block — which is the point. The block only
runs when the selection passed. There is no way to reach Analyse with unvalidated IDs, because the
call sits inside the branch that proves they were validated.

1. **Ask the agent.** `AnalyseAgent.Create(client).RunAsync<CrashAnalysis>(AnalyseAgent.Prompt(prompt, selected))`.
2. **Take the result without trusting it.** `Gates.TryTyped(analyse)`.

`ValidateAnalysis` then rejects a blank finding, null action or question lists, and a confidence
outside 0–100, and returns `LowConfidence` below 60. The caution branch is a printed gate, not a
softer sentence in the finding — the model does not get to grade its own work.

> If you see `confidence: 0` on every run, your instruction never asked for a 0–100 confidence. The
> model is not being cautious; it is filling in a field nobody told it about.

<details>
<summary><b>Stuck? The whole thing</b></summary>

```csharp
var analyse = await AnalyseAgent.Create(client).RunAsync<CrashAnalysis>(AnalyseAgent.Prompt(prompt, selected));
analysis = Gates.TryTyped(analyse);
```
</details>

## Run it

```bash
dotnet build
dotnet run -- "Show up to 5 intersection crashes from 2012."
dotnet run -- "Find cyclist crashes."
```

The first should print both gates as `Supported` and a grounded finding. The second gathers 0 records,
so neither Extract nor Analyse is called.

## Configuration

Same as labs 01–04: defaults in `appsettings.json`, overridden by `dotnet user-secrets`, overridden by
shell variables.

**AI unblock prompt:** `Complete only CP-05 in this lab. In Program.cs at the CP-05 marker, call AnalyseAgent.Create(client).RunAsync<CrashAnalysis>(AnalyseAgent.Prompt(prompt, selected)) and assign analysis = Gates.TryTyped(analyse). Pass only selected, never the evidence pack or the corpus. Keep it tool-free. Do not change Gates.cs, Utilities.cs, Models.cs or the agent files, and do not add files or projects. Run dotnet build and dotnet run -- "Show up to 5 intersection crashes from 2012."`
