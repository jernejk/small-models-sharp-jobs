# Workshop repository guidance

This repository is a build-along for people and coding agents. Attendees work from one named project
under [`workshop/`](workshop/), not the facilitator tree. The delivery checkpoint map is
[`facilitator/workshop/CHECKPOINTS.md`](facilitator/workshop/CHECKPOINTS.md).

## Safe working rules

- Start in the named numbered project under `workshop/` for the attendee path. The completed recovery
  reference is `facilitator/reference/solution/`. There is intentionally no runnable root solution.
- Keep Gather deterministic: it may use only the approved dataset, date/term filters and a bounded
  result count. Do not add arbitrary paths, network fetches, databases, or model-selected files.
- Keep Extract and Analyse separate. Do not depend on a combined local tool-call plus schema output
  request; validate every model-shaped result in code before moving on.
- Never add keys or real incident data. Configuration is environment-variable names and placeholders
  only. A participant-owned hosted endpoint is optional, never a workshop requirement.
- Preserve failing tests until the checkpoint is intentionally complete. Run the narrow test first,
  then `dotnet test`; report what was actually run.

## Local model lane

Use any already-running OpenAI-compatible local server. The normal choices are Ollama or LM Studio;
select the endpoint and model with `MAF_ENDPOINT` and `MAF_MODEL` (see `facilitator/workshop/SETUP.md`). If no
server is available, do not invent installation commands or download a large model during the
workshop: use the saved-output recovery lane and research the participant's preferred runtime.

Suggested starting points are deliberately conservative: a small 4B-class instruction model for
narrow Gather/Extract exercises; a 12B-class model for richer explanations on a comfortable machine;
and a 27B-class model only after the smaller path works. Model names, quantisation, memory, and tool
support vary by build, so run `ready` (or the named checkpoint) rather than treating this as a
compatibility guarantee.

## Slide visual QA

The HTML deck can be opened directly from `facilitator/workshop/slides/index.html`; no local web server is needed
for normal presenter review. Use the platform `open` command to launch that file in Safari, then
visually check the relevant slide at presentation size before claiming a layout change is complete.
Use a local server only when testing a browser behavior that the file URL cannot exercise.

## A good coding-agent handoff

State the checkpoint name, target files, acceptance command, and constraints. For example:

> Complete 03-gather only. Change `workshop/03-gather/src/Workshop.Core/IncidentDataset.cs`; preserve the
> approved-dataset boundary; run the checkpoint's focused tests and report the result.

Do not ask an agent to use credentials, publish the repository, or replace the deterministic flow
with an autonomous loop.
