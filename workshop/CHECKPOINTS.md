# Checkpoints

Work from one checkpoint at a time. The bundled Victorian crash sample is the only core corpus.
CP-01, CP-02 and CP-04 onwards need an endpoint configured (CP-03 Gather never calls the model) — `dotnet user-secrets --project src/Workshop.App set
MAF_ENDPOINT ...`, per [SETUP.md](SETUP.md).

| Checkpoint | Outcome | Acceptance |
|---|---|---|
| CP-01 | Local hello | `smoke` echoes the exact token; a down endpoint or unloaded model exits non-zero. |
| CP-02 | Typed JSON | `typed` prints raw JSON then the parsed contract; malformed output exits non-zero. |
| CP-03 Gather | Code filters the approved crash sample by date, term and cap. | `gather --term intersection`; then a no-result query. |
| CP-04 Extract | Question plus compact pack becomes typed selected record IDs and confidence. | Unknown/duplicate IDs and malformed output are rejected in code. |
| CP-05 Analyse | Only validated selected records reach the analysis call. | Low confidence takes the caution branch. If you see `confidence: 0` on every run, your instruction never asked for a 0-100 confidence — the model is not being cautious, it is filling a field it was not told about. |
| CP-06 Workflow | Explicit calls become the same linear fixed workflow. | No evidence bypasses Extract and Analyse. |

The negative paths in the acceptance column (malformed output, unknown or duplicate IDs, low confidence) are not
something you can make the model do on demand; they are exercised by `tests/Workshop.Core.Tests/CrashWorkflowTests.cs` —
read those tests, or run `dotnet test` and watch them pass.

Fell behind? `workshop/checkpoints/` has a `CrashPipeline.cs` for CP-03 (stubs), CP-04 (Extract done) and CP-05 (both done) — copy one over `starter/src/Workshop.App/CrashPipeline.cs`.

Presenter note: `starter/` and `solution/` are generated from `src/`; `python3 scripts/generate-starter.py --check`
proves they match. Attendees edit `starter/` only and never need to run this.
