# 02 — Typed JSON

Hello is now complete. Add one small typed command contract: `Mute the microphone.` becomes action
`mute` and target `microphone`, not plausible-looking prose.

Still one project and one file. `SimpleCommand` is already declared for you; ask the model for it.

**Change only:** `Workshop.App/Program.cs`. Defaults live in `Workshop.App/appsettings.json`; `dotnet user-secrets` and shell variables override them.

```bash
dotnet build
dotnet run --project Workshop.App
```

Expected: `WORKSHOP_OK` as in lab 01, then `action: mute` and `target: microphone`.

**AI unblock prompt:** `Complete only CP-02 in this lab. In Workshop.App/Program.cs, add a second agent from the same chat client, call RunAsync<SimpleCommand>("Mute the microphone."), and print the action and target from response.Result. Keep it tool-free, keep the existing hello call, and do not add files, projects or keys. Run dotnet build and dotnet run --project Workshop.App.`
