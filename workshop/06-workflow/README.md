# 06 — Workflow

This final snapshot is the complete, explicit C# workflow: typed QueryAgent → C# validate/filter → Extract → validate → Analyse → validate. It intentionally avoids an unverified framework-workflow runtime API; the point is that a known safe sequence stays readable and debuggable.

```bash
dotnet build
dotnet run --project src/Workshop.App -- run --prompt "Show up to 5 intersection crashes from 2012."
dotnet run --project src/Workshop.App -- workflow --prompt "Show up to 5 intersection crashes from 2012."
dotnet run --project src/Workshop.App -- workflow --prompt "Find cyclist crashes."
```

Expected: both supported-prompt commands report `gate: Supported`; the no-evidence prompt exits cleanly without calling Extract or Analyse.

**AI unblock prompt:** `Review only lab 06. Do not replace the explicit C# sequence with an unverified Agent Framework workflow API. Verify the run and workflow acceptance commands and report any mismatch without editing unrelated files.`
