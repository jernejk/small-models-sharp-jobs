# Rehearsal status

Verified without a model: every numbered lab and the facilitator reference build; deterministic core tests and offline model-lane tests pass; Gather returns both a bounded supported Victorian crash pack and a clean empty pack.

Still required: run `cd workshop/06-workflow && dotnet run --project src/Workshop.App -- ready --prompt "Show up to 5 intersection crashes from 2012."` against the actual loaded presenter model. Until that succeeds, Extract -> Analyse is implementation-complete but not model-rehearsed.
