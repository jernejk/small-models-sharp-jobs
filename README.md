# Small Models, Sharp Jobs

An 85-minute build-along for a local, evidence-grounded crash-review workflow.

The core is intentionally fixed and visible:

```text
deterministic Gather -> typed Extract -> code gate -> typed Analyse -> code gate
```

Gather filters only the bundled, attributed Victorian road-crash sample by date and keyword. It is
ordinary code, not an arbitrary filesystem tool. Extract gets the attendee question and the compact
evidence pack, and may select only record IDs in that pack. Analyse gets only code-validated selected
records. Invalid IDs, malformed output, low confidence, and no evidence all stop or take a caution
branch. The portable local path deliberately does **not** combine tools and typed JSON in one call.

## Start here

```bash
dotnet test
dotnet run --project src/Workshop.App -- gather --term intersection
```

Once a participant-owned OpenAI-compatible local server is running (Ollama, LM Studio, or another
compatible runtime), set `MAF_ENDPOINT`, `MAF_MODEL`, and if required `MAF_API_KEY`, then run:

```bash
dotnet run --project src/Workshop.App -- run --term intersection
dotnet run --project src/Workshop.App -- workflow --term intersection
```

`ready` is the model-backed rehearsal command. It must complete a supported run on the actual loaded
model before it is described as rehearsed. Saved Gather outputs and delivery guidance are in
[`workshop/`](workshop/). The source and attribution for the teaching corpus are in
[`workshop/data/README.md`](workshop/data/README.md).

`starter/` and `solution/` are generated from `src/`; run `python3 scripts/generate-starter.py` after
changing a marked TODO, then verify with `python3 scripts/generate-starter.py --check`.
