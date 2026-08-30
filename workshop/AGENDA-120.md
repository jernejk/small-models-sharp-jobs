# Archived 120-minute agenda — not the delivery path

> This historical fictional-evidence version is retained for reference only. Do not use it for the Victorian crash workshop; use `AGENDA-85.md`.

# 120-minute agenda

Attendees complete an evidence lookup tool, connect a typed extraction step, add one deterministic
verification rule, then run the application to create a claim ledger, verification report and cited
incident brief.

**Core complete by minute 90.** The last 30 minutes are for breaking the pipeline and inspecting
verification — that is the payoff, not filler. Protect it.

Same four TODOs as the 60-minute version, with room to explain *why* and to let attendees write
a second rule of their own.

| Time | Segment | Notes |
| --- | --- | --- |
| 0–8 | **Setup check and the pitch** | `dotnet test` → 55 passed / 87 failed. State the thesis: small models do real work given narrow jobs, tools, workflows and verification. |
| 8–18 | **The evidence pack** | Read all four files aloud. Surface the conflict: email blames billing, `events.csv` shows a stale routing rule. Ask what a brief should do with that. Park the answer. |
| 18–30 | **Architecture** | Draw the pipeline. Be explicit about the trust boundary: model produces claims, code decides truth. Explain why the answer key is unreadable by the tool. |
| 30–45 | **TODO 1 — the tool** | Whitelist vs path check, and why a whitelist survives inputs you did not imagine. `dotnet test --filter EvidenceStoreTests` → 14 passed (whole suite: 55 → 83). |
| 45–62 | **TODO 2 + 3 — the model's one job** | Register the tool; connect typed extraction. Demonstrate the combined-call failure live (see [DEMOS.md](DEMOS.md), demo 3) — 1.4s and an empty array. Discuss what that implies for agent design on small models. |
| 62–80 | **TODO 4 — the verifier** | Write `R2-QUOTE-PRESENT`. Then walk the other rules: numbers and timestamps compare against facts parsed independently from source, never against the model. `dotnet test` → 142 passed. |
| 80–90 | **Run it** | Full path, ~20 seconds. Read `incident-brief.md` together. Land `UNVERIFIED` and the code-parsed timeline. **Core is now complete.** |
| 90–105 | **Break it, three ways** | All three seeded defects, then hand-edited quotes and numbers in `claim-ledger.json`. Have them predict the rule ID before running. Being right is the learning. |
| 105–115 | **Write your own rule** | Free exercise: a rule for `duration`, or "every required claim must cite at least two sources". Real code, their design. |
| 115–120 | **Close and escalate** | Optionally show the same binary against a hosted endpoint with only env vars changed — same schema, same verifier, different model. Then the honest limits: [CLAIMS-AND-LIMITS.md](CLAIMS-AND-LIMITS.md). |

## Extensions, in priority order

Use only after minute 90, only if the room is ahead:

1. **Write your own rule** (scheduled above) — highest value, always land this one.
2. **Provider swap** — change `MAF_ENDPOINT`/`MAF_MODEL`, rerun, diff the ledger. Shows the seam is
   real. Requires the organiser endpoint; fictional evidence only.
3. **Model swap** — `MAF_MODEL=qwen3.5:4b`. Discuss why a model can pass typed extraction and still
   drop a required fact from prose. Attendees must have pulled it beforehand.
4. **Second evidence source through the model** — add `runbook.md` to `ProseSources` and watch the
   run time climb. Concrete lesson in why you do not send code-parsable files to a model.

## Checkpoints

| By minute | Everyone should have |
| --- | --- |
| 8 | tests running |
| 45 | 83 passing |
| 80 | 142 passing |
| 90 | three artifacts — **core complete** |
| 105 | seen defects caught and named the rule |

If minute 90 arrives and the room is not green, stop teaching and get everyone to green by copying
from `solution/`. The last 30 minutes are worth more than TODO 2.
