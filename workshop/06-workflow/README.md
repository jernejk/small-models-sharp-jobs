# 06 — Workflow

Everything is implemented. This lab is the same Gather → Extract → Analyse sequence you built by hand, expressed with the **real MAF Workflows API** — `Executor<TIn, TOut>` nodes joined by conditional edges.

```bash
dotnet build
dotnet run -- "Show up to 5 intersection crashes from 2012."
dotnet run -- "Find cyclist crashes."
```

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
