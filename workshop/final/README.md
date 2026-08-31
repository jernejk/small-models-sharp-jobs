# final — the completed pipeline

Every checkpoint implemented. Use this if you fall behind, or to diff your lab against a working answer.

```bash
dotnet build
dotnet run -- "Show up to 5 intersection crashes from 2012."
dotnet run -- "Show up to 5 cyclist crashes."
```

This is labs 01–05 finished: Gather fills an untrusted filter that C# validates, Extract selects only from the gathered pack, a code gate checks every id, and Analyse only ever sees records that cleared the gate.

[`06-workflow`](../06-workflow/) is this same pipeline expressed with the MAF Workflows API — `Executor<TIn, TOut>` nodes and conditional edges instead of statements.

Read `Program.cs` top to bottom; it is the whole flow.
