# Reference solution — facilitator only

The finished pipeline with every TODO completed. It exists for recovery: if a lab goes wrong on the
day, diff against this tree. Attendees start in [`workshop/`](../../../workshop/) and never need it.

Code-identical to [`workshop/06-workflow`](../../../workshop/06-workflow/) apart from this README.

```bash
dotnet build && dotnet test
dotnet run --project src/Workshop.App -- run --prompt "Show up to 5 intersection crashes from 2012."
dotnet run --project src/Workshop.App -- workflow --prompt "Show up to 5 intersection crashes from 2012."
dotnet run --project src/Workshop.App -- workflow --prompt "Find cyclist crashes."
```

Expected: `Workshop.Core.Tests` 11 passed and `Workshop.LocalModel.Tests` 22 passed with 5 skipped;
both supported prompts report `gate: Supported`; the cyclist prompt reports `gate: NoEvidence` and
exits 0 without calling Extract or Analyse.
