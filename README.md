# Small Models, Sharp Jobs

An 85-minute build-along for a local, evidence-grounded crash-review workflow.

## Start here: attendee labs

The hands-on path is [`workshop/`](workshop/). It contains six independent, progressive projects;
open `01-getting-started` first and move forward one stage at a time. Each lab contains the minimum
scaffold and its own acceptance command.

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
