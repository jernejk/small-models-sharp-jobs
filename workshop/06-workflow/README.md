# 06 — Workflow

Everything is implemented. This lab is the same Gather → Extract → Analyse sequence you built by hand, expressed with the **real MAF Workflows API** — `Executor<TIn, TOut>` nodes joined by conditional edges.

```bash
dotnet build
dotnet run -- "Show up to 5 intersection crashes from 2012."
dotnet run -- "Find cyclist crashes."
```

## From lab 05 to here

In lab 05 a step was two things in two places: an `ExtractAgent` file holding the instructions, and three statements in `Program.cs` calling it. Here a step is **one class**.

**Lab 05 — the agent is over there, the call is in `Program.cs`:**

```csharp
var extract = await ExtractAgent.Create(client).RunAsync<CrashSelection>(ExtractAgent.Prompt(prompt, evidence));
var selection = Gates.TryTyped(extract);
var gate = Gates.ValidateSelection(evidence, selection, out var selected);

if (gate is CrashGate.Supported)
{
    // ... and it is on you to remember Analyse must not run otherwise
}
```

**Lab 06 — instructions, prompt and call live together in the executor:**

```csharp
internal sealed class ExtractExecutor(IChatClient client, string prompt)
    : Executor<CrashRun, CrashRun>("extract")
{
    private readonly AIAgent _agent = Agents.Create(client, "ExtractAgent", """
        Pick the records from the evidence pack that answer the question.
        Copy recordIds exactly as they appear in the pack. Never invent an id.
        ...
        """);

    public override async ValueTask<CrashRun> HandleAsync(CrashRun run, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var extract = await _agent.RunAsync<CrashSelection>(
            $"Question: {prompt}\nEvidence pack JSON:\n{Utilities.ToJson(run.Evidence)}", cancellationToken: cancellationToken);

        var selection = Gates.TryTyped(extract);
        var gate = Gates.ValidateSelection(run.Evidence, selection, out var selected);
        return run with { Selection = selection, Selected = selected, Gate = gate is CrashGate.Supported ? null : gate };
    }
}
```

The three working lines are unchanged. Three things did change:

1. **One step, one class.** The instructions, the prompt it sends and the gate it applies are in one place. `Executors.cs` holds all four steps, so the whole pipeline is one file.
2. **The signature is the contract.** `Executor<CrashRun, CrashRun>` says what goes in and what comes out, and a step can only be wired to something whose types line up.
3. **The `if` is gone.** Instead of remembering to skip Analyse, the executor sets `Gate` and the *graph* decides what runs next. You stop writing control flow and start declaring it.

## What changed from lab 05

Lab 05 ran the steps as plain C# statements. Here each step is an executor:

| Executor | In → Out |
|---|---|
| `GatherExecutor` | `string` → `CrashRun` |
| `ExtractExecutor` | `CrashRun` → `CrashRun` |
| `AnalyseExecutor` | `CrashRun` → `CrashRun` |
| `ReportExecutor` | `CrashRun` → `CrashGate` |

`CrashRun` travels the edges. A non-null `Gate` means "stop and report", which is what makes the shape safe:

```csharp
var workflow = new WorkflowBuilder(gather)
    .AddEdge<CrashRun>(gather,  report,  run => run is { Gate: not null })
    .AddEdge<CrashRun>(gather,  extract, run => run is { Gate: null })
    .AddEdge<CrashRun>(extract, report,  run => run is { Gate: not null })
    .AddEdge<CrashRun>(extract, analyse, run => run is { Gate: null })
    .AddEdge(analyse, report)
    .WithOutputFrom(report)
    .Build();
```

There is no edge from `gather` to `analyse`. The graph itself makes "skip Extract" unrepresentable — you cannot forget a gate, because forgetting one is not a path.

`dotnet run -- "Find cyclist crashes."` prints `invoked: gather` then `invoked: report`. Extract and Analyse are never constructed, let alone called.

## The gotcha worth knowing

`InProcessExecution.RunAsync` **does not throw when an executor fails.** The exception arrives as a `WorkflowErrorEvent` on the run. Skip that check and a dead endpoint gives you no output, no error, and exit code 0 — a broken run that looks like a clean one.

```csharp
if (run.OutgoingEvents.OfType<WorkflowErrorEvent>().Select(e => e.Data).OfType<Exception>().FirstOrDefault() is { } failure)
```

Try it:

```bash
MAF_ENDPOINT=http://localhost:9/v1 dotnet run -- "Show up to 5 intersection crashes from 2012."
```

You should see `workflow error: Connection refused`. Comment out the check and you see nothing at all. A workflow engine gives you composition and a routing graph; it does not give you failure handling for free.
