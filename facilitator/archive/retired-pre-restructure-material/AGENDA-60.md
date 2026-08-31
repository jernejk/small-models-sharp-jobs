# Archived 60-minute agenda — not the delivery path

> This historical fictional-evidence version is retained for reference only. Do not use it for the Victorian crash workshop; use `AGENDA-85.md`.

# 60-minute agenda

Attendees complete an evidence lookup tool, connect a typed extraction step, add one deterministic
verification rule, then run the application to create a claim ledger, verification report and cited
incident brief.

**Core must be done by minute 55.** Everything below is cut to protect that.

Attendees start in `starter/`, which compiles and has four TODOs. No blank-project build.

**Cut from this version:** model comparison, routing theory, Harness, DevUI, LM Studio parity, the
cloud escalation demo. If you find yourself explaining any of them, you are late.

Measured machine time across the whole path is about 32 seconds of waiting (one 20-second model run,
the rest builds and tests). The clock below is human time.

| Time | Segment | Facilitator cue |
| --- | --- | --- |
| 0–4 | **Setup check** | "Run `dotnet test` in `starter/`. You want **55 passed, 87 failed**." Anyone red on the build goes to the recovery lane *now*, not at minute 30. |
| 4–9 | **The problem** | Show `evidence-pack/`. Ask: "the customer email blames the billing system, the event log says stale routing rule — which one goes in the brief?" Do not answer it. Show the three artifacts from a finished run instead. |
| 9–17 | **TODO 1 — the tool** | Whitelist, not path-checking. Point at `expected-facts.json` sitting unreadable in the same folder. Cue: `dotnet test --filter EvidenceStoreTests` → **14 passed** (whole suite: 55 → 83). |
| 17–28 | **TODO 2 + 3 — model does one job** | Register the tool, then the typed call. Say plainly: structured output and tools **cannot** be combined on this model — it returns `{"claims": []}` in 1.4s. That constraint is the lesson. Cue: build succeeds. |
| 28–37 | **TODO 4 — the verifier** | "Who decides this is true?" Write `R2-QUOTE-PRESENT`. Cue: `dotnet test` → **142 passed**. This is the moment the room goes green. |
| 37–45 | **Run it** | `dotnet run --project src/Workshop.App -- run`. ~20 seconds — talk over it. Then read `incident-brief.md` **together**, out loud. Land the two observations: the cause claim is `UNVERIFIED`, and the timeline came from code. |
| 45–53 | **Break it** | `verify-only --inject-defect altered-number` → exit 2. Then have them hand-edit a quote in `claim-ledger.json` and watch *their own rule* catch it. This is the best five minutes of the workshop; do not let it get squeezed. |
| 53–58 | **So what** | Narrow job, constrained tool, deterministic verification, deterministic render. Name what did *not* happen: no vector database, no second model grading the first, no agent framework theatre. |
| 58–60 | **Close** | Point at `solution/`, `CLAIMS-AND-LIMITS.md` and the recovery lane. |

## Checkpoints

Call these out loud. If the room is not here, use the five-minute rule (see
[FACILITATOR-RUNBOOK.md](../../workshop/FACILITATOR-RUNBOOK.md)).

| By minute | Everyone should have |
| --- | --- |
| 4 | `dotnet test` running at all |
| 17 | 83 passing (14 on the filtered EvidenceStoreTests run) |
| 37 | 142 passing |
| 45 | three files in `artifacts/` |
| 53 | seen a defect caught |

## If you are running late

Cut in this order:

1. The hand-edit half of **Break it** (keep `--inject-defect`, drop the manual edit).
2. **So what** down to two sentences.
3. TODO 2+3 — tell them to copy from `solution/` and spend the time on TODO 4 instead.

Never cut **Run it**. An attendee who leaves without seeing their own three artifacts did not attend
this workshop.
