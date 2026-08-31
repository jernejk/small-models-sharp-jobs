# 02 — Typed JSON

Hello is now complete. Add one small typed command contract: `Mute the microphone.` becomes action
`mute` and target `microphone`, not plausible-looking prose.

Still one project and one file. `SimpleCommand` is already declared at the bottom of `Program.cs`; ask the model for it.

**Change only:** `Workshop.App/Program.cs`, at the CP-02 marker.

## What you're writing

The hello call above it is your template. A typed call differs in two places:

1. **Ask for a type.** `RunAsync<SimpleCommand>("Mute the microphone.")` instead of `RunAsync(...)`.
2. **Read the parsed result.** `response.Result` is the `SimpleCommand`; `response.Text` is still the raw JSON the model produced. Print both — seeing the JSON next to the parsed object is the point.

Give this agent its own name and its own instructions: convert the request into one command with an action and a target.

<details>
<summary><b>Stuck? The whole thing</b></summary>

```csharp
AIAgent typedAgent = client.AsAIAgent(
    instructions: "Convert the request into one small command with an action and a target.",
    name: "TypedAgent");

var typed = await typedAgent.RunAsync<SimpleCommand>("Mute the microphone.");
Console.WriteLine("raw: " + typed.Text);
SimpleCommand command = typed.Result;
Console.WriteLine($"action: {command.Action}");
Console.WriteLine($"target: {command.Target}");
```
</details>

## Run it

```bash
dotnet build
dotnet run --project Workshop.App
```

Expected: `WORKSHOP_OK` as in lab 01, then the raw JSON, then `action: mute` and `target: microphone`.

A clean parse is not a content check — the type only guarantees the shape, never that the values are right. That distinction is what the gates in lab 04 exist for.

**AI unblock prompt:** `Complete only CP-02 in this lab. In Workshop.App/Program.cs, add a second agent from the same chat client, call RunAsync<SimpleCommand>("Mute the microphone."), and print the raw text and the parsed action and target from response.Result. Keep it tool-free, keep the existing hello call, and do not add files, projects or keys. Run dotnet build and dotnet run --project Workshop.App.`
