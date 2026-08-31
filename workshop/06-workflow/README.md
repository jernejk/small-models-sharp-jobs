# 06 — Workflow

Everything is implemented. This lab is the same Gather → Extract → Analyse sequence you built by hand, expressed with the **real MAF Workflows API** — `Executor<TIn, TOut>` nodes joined by conditional edges.

```bash
dotnet build
dotnet run -- "Show up to 5 intersection crashes from 2012."
dotnet run -- "Find cyclist crashes."
```

## From lab 05 to here

Nothing about the agents changed. `GatherAgent`, `ExtractAgent` and `AnalyseAgent` are the same files, with the same instructions. What changed is who calls them.

**Lab 05 — you call them, in order, and remember the gate yourself:**

```csharp
var extract = await ExtractAgent.Create(client).RunAsync<CrashSelection>(ExtractAgent.Prompt(prompt, evidence));
var selection = Gates.TryTyped(extract);
var gate = Gates.ValidateSelection(evidence, selection, out var selected);

if (gate is CrashGate.Supported)
{
    // ... and it is on you to remember that Analyse must not run otherwise
}
```

**Lab 06 — the same three lines, moved inside an executor:**

```csharp
internal sealed class ExtractExecutor(IChatClient client, string prompt)
    : Executor<CrashRun, CrashRun>("extract")
{
    public override async ValueTask<CrashRun> HandleAsync(CrashRun run, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var extract = await ExtractAgent.Create(client).RunAsync<CrashSelection>(ExtractAgent.Prompt(prompt, run.Evidence), cancellationToken: cancellationToken);
        var selection = Gates.TryTyped(extract);
        var gate = Gates.ValidateSelection(run.Evidence, selection, out var selected);
        return run with { Selection = selection, Selected = selected, Gate = gate is CrashGate.Supported ? null : gate };
    }
}
```

The body is identical. The two differences are the whole point:

1. **The signature is the contract.** `Executor<CrashRun, CrashRun>` says what goes in and what comes out. A step can only be wired to something whose types line up.
2. **The `if` is gone.** Instead of remembering to skip Analyse, the executor sets `Gate` and the *graph* decides what happens next. You stop writing control flow and start declaring it.

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
