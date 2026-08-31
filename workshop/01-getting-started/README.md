# 01 — Getting started

Make one local model call that replies with an exact token. This proves the endpoint, model name, and Agent Framework connection before structure or data.

**Change only:** `src/Workshop.App/CrashPipeline.cs`, `HelloAsync`.

```bash
dotnet build
dotnet run --project src/Workshop.App -- smoke
```

Expected: `reply: WORKSHOP_OK`; an unavailable endpoint or unloaded model exits non-zero with a local configuration hint.

**AI unblock prompt:** `Complete only CP-01 in this lab. Implement HelloAsync in src/Workshop.App/CrashPipeline.cs. Keep one tool-free exact-token call; do not change any other file or add keys. Run dotnet build and dotnet run --project src/Workshop.App -- smoke.`
