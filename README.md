# Small Models, Sharp Jobs

An 85-minute build-along for a local, evidence-grounded crash-review workflow.

## Start here: attendee labs

The hands-on path is [`workshop/`](workshop/). It contains six independent, progressive projects;
open `01-getting-started` first and move forward one stage at a time. Each lab is a single flat
project you open directly — no solution file, no subcommands, and the only input is a plain-English
prompt:

```bash
cd workshop/03-gather
dotnet build
dotnet run -- "Show up to 5 intersection crashes from 2012."
```

Each lab has one focused TODO and its own README, including the full snippet if you get stuck.
Configuration is the same everywhere: defaults in `appsettings.json`, overridden by
`dotnet user-secrets`, overridden by shell variables.

The fixed design remains visible throughout:

```text
deterministic Gather -> typed Extract -> code gate -> typed Analyse -> code gate
```

Gather filters only the bundled Victorian crash sample. It is ordinary code, not arbitrary filesystem
access. Extract and Analyse are separate, tool-free typed calls. Invalid IDs, malformed output, low
confidence, and no evidence stop or take a caution branch.

## Facilitator material

Slides, research, delivery notes, recovery runs, and the finished reference implementation are under
[`facilitator/`](facilitator/). They are deliberately separate from the attendee path.

The repository root intentionally contains no runnable root solution. Start in a numbered lab;
the finished facilitator-only recovery reference is `facilitator/reference/solution/`.
