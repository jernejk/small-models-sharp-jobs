# Checkpoint catch-up files

Copy the file for the checkpoint you want to be at over `starter/src/Workshop.App/CrashPipeline.cs`, then run the
acceptance command from [CHECKPOINTS.md](../CHECKPOINTS.md).

| File | State | `run --term intersection` |
|---|---|---|
| `cp-03-starter.CrashPipeline.cs` | both TODOs are stubs (identical to `starter/`) | `gate: UnsupportedSelection`, exit 2 |
| `cp-04-extract-done.CrashPipeline.cs` | TODO 4 done, TODO 5 still a stub | extract JSON, then `gate: UnsupportedAnalysis`, exit 2 |
| `cp-05-analyse-done.CrashPipeline.cs` | both done (identical to `solution/`) | `gate: Supported`, exit 0 |

```bash
cp workshop/checkpoints/cp-04-extract-done.CrashPipeline.cs starter/src/Workshop.App/CrashPipeline.cs
```

Derived from `starter/` and `solution/`; regenerate after changing the TODO regions in `src/`.
