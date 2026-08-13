# Demo scripts

Every demo lists **what to say**, **what to run** and **what you should see**. If what you see does
not match, say so out loud — a facilitator narrating a surprise is more convincing than one reciting
a script.

All timings measured on the reference machine (see [CLAIMS-AND-LIMITS.md](CLAIMS-AND-LIMITS.md)).

---

## Demo 1 — the clean run (5 minutes)

**Say:** "Four evidence files go in. Three artifacts come out. A 4-billion-parameter model on a
laptop wrote exactly one of them, and not the one you would expect."

```bash
dotnet run --project src/Workshop.App -- run
```

**You should see** (~22–25 s; the timing line varies by a second or two, the counts do not):

```text
tool calls    : [status.txt, customer-email.txt] contract=held
claims        : 8
verification  : 33 passed, 0 failed, 1 unverified
timing        : gather 3.9s + extract 18.6s = 22.5s
```

Now open `artifacts/incident-brief.md` and read these aloud:

1. **Verified facts** — every row carries a source and an exact quote. Nothing appears here without
   both.
2. **Shown but not verified** — the customer's line about the billing system, tagged
   `R9-CAUSE-UNVERIFIED`. Then point at the timeline: the actual trigger was a stale routing rule.
   **Say:** "The model reported the email accurately. Code refused to promote a customer's guess to
   a fact. Neither of those is an accident."
3. **Timeline** — parsed from `events.csv` by ordinary code. The model never saw that file.

**Say:** "One unverified item is not a defect. A system that only says pass or fail will quietly
promote everything it cannot actually check."

---

## Demo 2 — break it (8 minutes, the best part)

**Say:** "Verification you have never seen fail is decoration. Let's fail it three ways."

Ask the room to predict the rule ID before each run.

```bash
dotnet run --project src/Workshop.App -- verify-only --inject-defect phantom-source
dotnet run --project src/Workshop.App -- verify-only --inject-defect altered-number
dotnet run --project src/Workshop.App -- verify-only --inject-defect altered-timestamp
```

**You should see** exit code 2 each time, and:

| Defect | Rule that catches it | What it simulates |
| --- | --- | --- |
| `phantom-source` | `R1-SOURCE-WHITELIST` | a citation to a file that does not exist |
| `altered-number` | `R5-AFFECTED-CUSTOMERS` | a number drifting from the source |
| `altered-timestamp` | `R6-TIMESTAMP` | a plausible time that appears nowhere in evidence |

Each fails on **exactly one** rule. **Say:** "Catching a problem for the wrong reason is not
catching it."

Then the live one — this is what people remember:

1. Open `artifacts/claim-ledger.json`.
2. Change any `exactQuote` to something not in the source, e.g. `"Severity: SEV-1 catastrophic"`.
3. `dotnet run --project src/Workshop.App -- verify-only`

**You should see** `R2-QUOTE-PRESENT` fail — the rule the attendees wrote themselves — and the claim
disappear from **Verified facts** into **Excluded by verification**, named, not silently dropped.

---

## Demo 3 — why the model gets two calls, not one (4 minutes)

Use when someone asks why the pipeline looks over-engineered. Cut this first if short on time.

**Say:** "The obvious design is one agent with a tool that returns typed output. Here is why this
repo does not do that."

Show `IncidentPipeline.CreateAgent` — the extraction agent is created with `tools: null` — then:

```bash
dotnet run --project src/Workshop.App -- run
```

Point at the timing line: `gather` and `extract` are separate model calls.

**Say:** "On this model, asking for structured output while offering tools returns an empty claims
array in about 1.4 seconds and never calls the tool. It does not error. It just quietly does
nothing. So the pipeline splits the job: one call fetches through the tool, one extracts typed
claims from what came back."

**The point:** a small model forces you to separate concerns you could have been sloppy about with a
frontier model. That separation is also easier to test, and each step fails visibly instead of
silently.

---

## Demo 4 — the gates (3 minutes, facilitator confidence)

**Say:** "How would you know this model is good enough before standing in front of a room?"

```bash
dotnet run --project src/Workshop.App -- gates --repeat 5
```

**You should see** (~2 minutes) `OUTCOME: PASS`, with L1 smoke, L2 typed extraction 5/5, L3 tool
contract 5/5, L4 integrated 5/5, three defects caught for the intended rule, and latency inside 30 s.

**Say:** "Five runs, not one. A single passing run tells you almost nothing about a model."

---

## Demo 5 — provider swap (3 minutes, 120-minute version only)

Requires the organiser endpoint. **Fictional evidence only.**

```bash
MAF_ENDPOINT=<organiser> MAF_MODEL=<organiser> dotnet run --project src/Workshop.App -- run
```

**You should see** the same three artifacts and the same verifier semantics. The ledger wording will
differ — different model, different phrasing.

**Say:** "The schema and the rules did not move. Only the thing producing claims did. That is what a
provider seam buys you — and it is why the verifier lives in a project that has no model dependency
at all."
