# 05 — Analyse

QueryAgent, C#-validated Gather, and Extract are complete; code has already validated selected IDs. Add the separate analysis call, which sees only validated records and takes a visible caution branch on bad or low-confidence output.

**Change only:** `src/Workshop.App/CrashPipeline.cs`, `AnalyseAsync`.

```bash
dotnet build
dotnet run --project src/Workshop.App -- run --prompt "Show up to 5 intersection crashes from 2012."
dotnet run --project src/Workshop.App -- run --prompt "Find cyclist crashes."
```

Expected: the supported prompt can reach `gate: Supported`; no evidence stops before Extract and Analyse. Malformed/low-confidence analysis takes the caution branch.

**AI unblock prompt:** `Complete only CP-05 in this lab. Implement AnalyseAsync in src/Workshop.App/CrashPipeline.cs as a tool-free typed CrashAnalysis call over only the already-validated selected records. Preserve all gates and do not change other files. Run build and the two run commands.`
