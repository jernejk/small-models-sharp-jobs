# 03 — Gather

Hello and typed JSON are complete. Now the model turns a plain-English prompt into a **filter**, C# validates that filter, and only then does deterministic code touch the approved crash sample.

The model never sees the corpus and never picks records. It fills in a small typed contract; your code decides what that contract is allowed to do.

Still one project, still flat — four files plus the data:

| File | What's in it |
|---|---|
| `Program.cs` | config, the chat client, and the visible flow |
| `GatherAgent.cs` | the agent and its instructions |
| `Models.cs` | `CrashRecord`, `QueryFilter`, `ModelSettings` |
| `Utilities.cs` | loading, validating and filtering — no agent touches this |

**Change only:** `Program.cs`, at the CP-03 marker.

## What you're writing

Three lines, and the middle one is the whole lesson:

1. **Ask the agent for a filter.** `await agent.RunAsync<QueryFilter>(prompt)` — same typed call as lab 02, new contract.
2. **Validate it.** `Utilities.ValidateFilter(response.Result)`. The model's filter is *untrusted*: it can invent a reversed date range, an 800-character term, or ask for 10,000 results. Validation swaps the dates, trims the term and clamps the cap to 1–20.
3. **Gather.** `Utilities.Gather(records, filter)` — plain LINQ over records already in memory.

Print the model's filter next to the validated one. Seeing them differ is the point.

<details>
<summary><b>Stuck? The whole thing</b></summary>

```csharp
var response = await agent.RunAsync<QueryFilter>(prompt);
Console.WriteLine("model filter:     " + Utilities.ToJson(response.Result));

var filter = Utilities.ValidateFilter(response.Result);
Console.WriteLine("validated filter: " + Utilities.ToJson(filter));

var evidence = Utilities.Gather(records, filter);
Console.WriteLine($"gathered: {evidence.Count} record(s)");
```
</details>

## Run it

The only input is the prompt. No flags, no subcommands.

```bash
dotnet build
dotnet run -- "Show up to 5 intersection crashes from 2012."
```

### Try these

| Prompt | You get | What it shows |
|---|---|---|
| `Show up to 5 intersection crashes from 2012.` | 5 records | A year becomes a date range. The model says `"intersection crash"`; the corpus says *"cross traffic (intersections only)"*, so validation strips `crash` before matching. |
| `Show up to 10 rear end crashes.` | 10 records | The largest category in the sample (205 records), capped where you asked. |
| `Show up to 5 cyclist crashes.` | **0 records** | There are no cyclist crashes here. An empty result is a correct answer, not a failure — the later labs stop rather than invent. |
| `Give me 500 crashes.` | 20 records | The model happily asks for 500. C# clamps it to 20. This is the gate doing its job where you can see it. |

Then write your own. **Say how many you want** — a prompt with no cap is at the model's mercy, and you will get a different count run to run.

Real categories in the sample: `rear end`, `cross traffic`, `right through`, `head on`, `struck animal`, `out of control`. Years run 2012–2025. Ask for something that is not there (`pedestrian`, `motorcycle`) and you should get 0 — that is the honest answer.

Run `dotnet run` with no argument and it uses the first prompt as a default.

## Configuration

Same as labs 01 and 02: defaults in `appsettings.json`, overridden by `dotnet user-secrets`, overridden by shell variables.

**AI unblock prompt:** `Complete only CP-03 in this lab. In Program.cs at the CP-03 marker, call agent.RunAsync<QueryFilter>(prompt), print the raw model filter, pass response.Result through Utilities.ValidateFilter, print the validated filter, then call Utilities.Gather(records, filter) and print the count. Keep it tool-free. Do not change Utilities.cs, Models.cs or GatherAgent.cs, and do not add files or projects. Run dotnet build and dotnet run -- "Show up to 5 intersection crashes from 2012."`
