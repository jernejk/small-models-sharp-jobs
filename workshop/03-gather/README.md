# 03 — Gather

Hello and typed JSON are complete. Add one tool-free typed QueryAgent that converts a natural-language question into an untrusted date range, term, and cap. C# validates that filter before deterministic in-memory Gather touches the approved local crash sample.

**Change only:** `src/Workshop.App/CrashPipeline.cs`, `InterpretQueryAsync`.

```bash
dotnet build
dotnet run --project src/Workshop.App -- query --prompt "Show up to 5 intersection crashes from 2012."
dotnet run --project src/Workshop.App -- gather --term definitely-not-present # deterministic debug check
```

Expected: the first command prints typed JSON then a C#-validated filter; the second is a deterministic debug check that reports an empty pack and does not call Extract or Analyse.

**AI unblock prompt:** `Complete only CP-03 in this lab. Implement InterpretQueryAsync in src/Workshop.App/CrashPipeline.cs as one tool-free typed QueryFilter call. It converts the question to from, to, term, and maxResults; C# ValidateFilter remains the authority that swaps invalid date order, trims term to 80 characters, and clamps max to 1..20. Do not add tools, filesystem access, network data, or change any other file. Run the two commands in the README.`
