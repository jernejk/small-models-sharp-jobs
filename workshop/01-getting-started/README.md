# 01 — Getting started

Make one local model call that replies with an exact token. This proves the endpoint, model name, and Agent Framework connection before structure or data.

This lab is deliberately bare: one project, one `Program.cs`, no data and no tests. Those arrive from lab 03.

**Change only:** `Workshop.App/Program.cs`, inside the `try` block.

## What you're writing

Three steps:

1. **A chat client.** `new OpenAIClient(...)` takes an `ApiKeyCredential` and `OpenAIClientOptions { Endpoint = ... }`, both from `settings`. Then `.GetChatClient(settings.Model).AsIChatClient()`.
2. **An agent.** `client.AsAIAgent(instructions: ..., name: ...)`. The instructions tell it to reply with exactly the token it is given, nothing else.
3. **One call.** `await agent.RunAsync("Reply with exactly this token: WORKSHOP_OK")`, then print `response.Text`.

<details>
<summary><b>Stuck? The whole thing</b> (this is slide 16)</summary>

```csharp
var client = new OpenAIClient(
        new ApiKeyCredential(settings.ApiKey),
        new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) })
    .GetChatClient(settings.Model)
    .AsIChatClient();

AIAgent agent = client.AsAIAgent(
    instructions: "Reply with exactly the token you are given.",
    name: "HelloAgent");

var response = await agent.RunAsync("Reply with exactly this token: WORKSHOP_OK");
Console.WriteLine(response.Text);
```
</details>

## Run it

```bash
dotnet build
dotnet run --project Workshop.App
```

Expected: the lane/endpoint/model line, then `WORKSHOP_OK`. If nothing answers, check `lms ps` or `ollama ps` and your `dotnet user-secrets`.

## Configuration

Defaults live in `Workshop.App/appsettings.json`. Override them with `dotnet user-secrets`, or with a shell variable for a one-off:

```bash
dotnet user-secrets --project Workshop.App set MAF_ENDPOINT http://localhost:1234/v1
MAF_MODEL=some-other-model dotnet run --project Workshop.App
```

Shell variables beat user-secrets, which beat `appsettings.json`.

**AI unblock prompt:** `Complete only CP-01 in this lab. In Workshop.App/Program.cs, inside the existing try block, build an OpenAIClient from the existing settings object, chain GetChatClient(...).AsIChatClient().AsAIAgent(...), run it with "Reply with exactly this token: WORKSHOP_OK", and print the reply. Keep one tool-free call; do not add files, projects or keys. Run dotnet build and dotnet run --project Workshop.App.`
