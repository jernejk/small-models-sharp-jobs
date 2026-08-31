# 01 — Getting started

Make one local model call that replies with an exact token. This proves the endpoint, model name, and Agent Framework connection before structure or data.

This lab is deliberately bare: one project, the two Agent Framework packages, and `ModelSettings`. Data, gates and tests arrive in the later labs.

**Change only:** `Workshop.App/Program.cs`.

```bash
dotnet build
dotnet run --project Workshop.App
```

Expected: the lane/endpoint/model line, then `WORKSHOP_OK`. If nothing answers, check `lms ps` or `ollama ps` and your `dotnet user-secrets`.

**AI unblock prompt:** `Complete only CP-01 in this lab. In Workshop.App/Program.cs, build an OpenAIClient from the existing ModelSettings, chain GetChatClient(...).AsIChatClient().AsAIAgent(...), run it with "Reply with exactly this token: WORKSHOP_OK", and print the reply. Keep one tool-free call; do not add files, projects or keys. Run dotnet build and dotnet run --project Workshop.App.`
