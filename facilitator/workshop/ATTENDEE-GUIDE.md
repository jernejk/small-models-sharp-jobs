# Attendee guide

You are building a small internal insurance-and-safety analyst assistant. It reviews a compact,
de-identified Victorian crash sample; it does not accept or decline claims.

1. Open `workshop/01-getting-started`, then move through one numbered lab at a time.
2. Run each lab's README acceptance command from that lab root.
3. Configure your already-running local OpenAI-compatible endpoint using the placeholder names in
   the lab's `.env.example` (see [SETUP.md](SETUP.md)).
4. In `workshop/06-workflow`, run both `run --prompt "Show up to 5 intersection crashes from 2012."` and `workflow --prompt "Show up to 5 intersection crashes from 2012."`.

**Done means both final commands exit 0 with `gate: Supported`.** The no-evidence query stops before
model work. The complete recovery reference is `facilitator/reference/solution/`.

Gather is code-owned and bounded. Extract receives the question and Gather result only. Analyse
receives only Extract records that passed code validation. If a model returns an unknown ID, bad JSON,
or low confidence, the program stops or gives a caution outcome; it does not improvise a conclusion.

If your machine is not ready, pair with someone or follow the saved-output recovery lane. The hosted
fallback is participant-owned and optional; see `ADVANCED-OPENAI-COMPATIBLE-RECOVERY.md`.
