# 02 — Typed JSON

Hello is now complete. Add one small typed command contract: `Mute the microphone.` becomes action
`mute` and target `microphone`, not plausible-looking prose.

**Change only:** `src/Workshop.App/CrashPipeline.cs`, `TypedAsync`.

```bash
dotnet build
dotnet run --project src/Workshop.App -- smoke
dotnet run --project src/Workshop.App -- typed
```

Expected: smoke prints `WORKSHOP_OK`; typed prints raw JSON plus parsed action/target. Invalid or malformed output exits non-zero.

**AI unblock prompt:** `Complete only CP-02 in this lab. Implement TypedAsync in src/Workshop.App/CrashPipeline.cs using a tool-free typed SimpleCommand call. The request is “Mute the microphone.” and the parsed fields are action mute and target microphone. Preserve HelloAsync and all other files. Run dotnet build, smoke, and typed.`
