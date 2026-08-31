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
dotnet run -- "Find cyclist crashes."
```

The first returns 5 records. The second returns **0** — there are no cyclist crashes in this sample, and that empty result is a correct answer, not a failure. Extract and Analyse in the later labs stop there rather than inventing something.

Run `dotnet run` with no argument and it uses the first prompt as a default.

## Configuration

Same as labs 01 and 02: defaults in `appsettings.json`, overridden by `dotnet user-secrets`, overridden by shell variables.

**AI unblock prompt:** `Complete only CP-03 in this lab. In Program.cs at the CP-03 marker, call agent.RunAsync<QueryFilter>(prompt), print the raw model filter, pass response.Result through Utilities.ValidateFilter, print the validated filter, then call Utilities.Gather(records, filter) and print the count. Keep it tool-free. Do not change Utilities.cs, Models.cs or GatherAgent.cs, and do not add files or projects. Run dotnet build and dotnet run -- "Show up to 5 intersection crashes from 2012."`
