# 04 — Extract

Gather is done: the model fills a filter, C# validates it, and deterministic LINQ produces a small
**evidence pack**. Now a second agent reads that pack and says which records answer the question.

It answers with record IDs — and IDs are the easiest thing in the world for a model to invent. So the
new file is not really `ExtractAgent.cs`. It's `Gates.cs`.

Still one project, still flat:

| File | What's in it |
|---|---|
| `Program.cs` | config, the chat client, and the visible flow |
| `GatherAgent.cs` | the filter agent from lab 03 |
| `ExtractAgent.cs` | **new** — the selection agent, and the pack it is allowed to see |
| `Models.cs` | `CrashRecord`, `QueryFilter`, `CrashQuery`, **`CrashSelection`** |
| `Gates.cs` | **new** — `CrashGate`, `ValidateSelection`, `TryTyped` |
| `Utilities.cs` | loading, validating and filtering — unchanged from lab 03 |

**Change only:** `Program.cs`, at the CP-04 marker.

## What you're writing

Two lines, inside the `if (evidence.Count > 0)` block:

1. **Ask the agent.** `ExtractAgent.Create(client).RunAsync<CrashSelection>(ExtractAgent.Prompt(prompt, evidence))`.
   Note what the prompt carries: `evidence`, not `records`. Extract never sees the corpus.
2. **Take the result without trusting it.** `Gates.TryTyped(extract)` returns `null` instead of throwing
   when the model's JSON does not fit the contract. A malformed reply is a gate outcome, not a crash.

Then read the line that was already there:

```csharp
var gate = Gates.ValidateSelection(evidence, selection, out var selected);
```

That is where the workshop actually happens. `ValidateSelection` rejects an ID the pack does not
contain, the same ID twice, an empty list, a blank rationale, and a confidence outside 0–100. Under 60
it returns `LowConfidence` — a real selection, flagged. `selected` only ever contains records the code
looked up itself, by ID, from the pack.

<details>
<summary><b>Stuck? The whole thing</b></summary>

```csharp
var extract = await ExtractAgent.Create(client).RunAsync<CrashSelection>(ExtractAgent.Prompt(prompt, evidence));
selection = Gates.TryTyped(extract);
```
</details>

## Run it

```bash
dotnet build
dotnet run -- "Show up to 5 intersection crashes from 2012."
dotnet run -- "Find cyclist crashes."
```

The first gathers 5 records and should reach `gate: Supported`. The second gathers **0**, so the
`if` never runs and the gate is `NoEvidence` — Extract is not called at all. Stopping is the answer.

You cannot make the model invent an ID on demand, so you will probably only see the happy path. Read
`Gates.cs` for the branches you did not trigger; `ValidateSelection` is 16 lines and it is the part that ships.

## Configuration

Same as labs 01–03: defaults in `appsettings.json`, overridden by `dotnet user-secrets`, overridden by
shell variables.

**AI unblock prompt:** `Complete only CP-04 in this lab. In Program.cs at the CP-04 marker, call ExtractAgent.Create(client).RunAsync<CrashSelection>(ExtractAgent.Prompt(prompt, evidence)) and assign selection = Gates.TryTyped(extract). Keep it tool-free. Do not change Gates.cs, Utilities.cs, Models.cs or the agent files, and do not add files or projects. Run dotnet build and dotnet run -- "Show up to 5 intersection crashes from 2012."`
