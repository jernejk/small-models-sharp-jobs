# Attendee guide

You are building a small internal insurance-and-safety analyst assistant. It reviews a compact,
de-identified Victorian crash sample; it does not accept or decline claims.

1. Run `dotnet test`.
2. Run deterministic Gather: `dotnet run --project src/Workshop.App -- gather --term intersection`.
3. Confirm a no-result branch: add `--term cyclist`.
4. Configure your already-running local OpenAI-compatible endpoint using the placeholder names in
   `.env.example` — recommended is `dotnet user-secrets --project src/Workshop.App set MAF_ENDPOINT ...`
   (see [SETUP.md](SETUP.md)) — then run `run --term intersection`.
5. Run `workflow --term intersection`: it is the same safe sequence in reusable form.

**Done means `run --term intersection` exits 0 with `gate: Supported`.** `dotnet test` is green
before you write a line — the two TODOs live in `Workshop.App`, which the deterministic tests do not
touch. With a model running, `WORKSHOP_LOCAL_MODEL=1 dotnet test` (27 tests) is the full check.

Gather is code-owned and bounded. Extract receives the question and Gather result only. Analyse
receives only Extract records that passed code validation. If a model returns an unknown ID, bad JSON,
or low confidence, the program stops or gives a caution outcome; it does not improvise a conclusion.

If your machine is not ready, pair with someone or follow the saved-output recovery lane. The hosted
fallback is participant-owned and optional; see `ADVANCED-OPENAI-COMPATIBLE-RECOVERY.md`.
