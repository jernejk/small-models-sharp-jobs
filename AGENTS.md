# Workshop repository guidance

This repository is a build-along for people and coding agents. Work from a named checkpoint rather
than attempting the whole application in one change. The checkpoint map is
[`workshop/CHECKPOINTS.md`](workshop/CHECKPOINTS.md).

## Safe working rules

- Start in `starter/` for the attendee path. `solution/` is the recovery reference; both are
  generated from canonical `src/`, so regenerate them with `python3 scripts/generate-starter.py`
  after changing a marked TODO in `src/`.
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
select the endpoint and model with `MAF_ENDPOINT` and `MAF_MODEL` (see `workshop/SETUP.md`). If no
server is available, do not invent installation commands or download a large model during the
workshop: use the saved-output recovery lane and research the participant's preferred runtime.

Suggested starting points are deliberately conservative: a small 4B-class instruction model for
narrow Gather/Extract exercises; a 12B-class model for richer explanations on a comfortable machine;
and a 27B-class model only after the smaller path works. Model names, quantisation, memory, and tool
support vary by build, so run `ready` (or the named checkpoint) rather than treating this as a
compatibility guarantee.

## A good coding-agent handoff

State the checkpoint name, target files, acceptance command, and constraints. For example:

> Complete CP-03 only. Change `src/Workshop.Core/IncidentDataset.cs` and its tests; preserve the
> approved-dataset boundary; run the checkpoint's focused tests and report the result.

Do not ask an agent to use credentials, publish the repository, or replace the deterministic flow
with an autonomous loop.
