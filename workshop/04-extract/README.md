# 04 — Extract

The tool-free QueryAgent and C#-validated deterministic Gather are complete. Add a focused, tool-free Extract agent that sees only the compact evidence pack and returns typed record IDs, rationale, and confidence; code owns the ID validation.

**Change only:** `src/Workshop.App/CrashPipeline.cs`, `ExtractAsync`.

```bash
dotnet build
dotnet run --project src/Workshop.App -- run --prompt "Show up to 5 intersection crashes from 2012."
dotnet run --project src/Workshop.App -- run --prompt "Find cyclist crashes."
```

Expected: a valid selection reaches the next gate; unknown/duplicate/malformed IDs stop before Analyse. A no-evidence prompt stops before Extract and Analyse.

**AI unblock prompt:** `Complete only CP-04 in this lab. Implement ExtractAsync in src/Workshop.App/CrashPipeline.cs as one tool-free typed CrashSelection call over the question and evidence pack. Preserve deterministic Gather and code validation. Do not change other files. Run build and both run commands.`
