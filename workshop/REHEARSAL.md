# Rehearsal status

Verified without a model: canonical solution builds; 9 deterministic core tests and 22 offline model-lane tests pass; generated `starter/` and `solution/` match canonical source and build; Gather returns both a bounded supported Victorian crash pack and a clean empty pack.

Still required: run `dotnet run --project src/Workshop.App -- ready --term intersection` against the actual loaded presenter model. Until that succeeds, Extract -> Analyse is implementation-complete but not model-rehearsed.
