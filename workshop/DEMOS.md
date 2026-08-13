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

**You should see** (~19–22 s; the timing line varies by a second or two, the counts do not):

```text
LOCAL run | model=nemotron-3-nano:4b | endpoint=http://localhost:11434/v1 | evidence=...
tool calls    : [status.txt, customer-email.txt] contract=held
claims        : 7
verification  : 30 passed, 0 failed, 2 unverified
timing        : gather 5.1s + extract 15.7s = 20.8s
```

`LOCAL` is derived from the endpoint, not printed from a setting. Point it at someone else's server
and it says `HOSTED`.

Now open `artifacts/incident-brief.md` and read these aloud:

1. **Verified facts** — every row carries a source and an exact quote. Nothing appears here without
   both.
2. **Shown but not verified** — *two* entries, for two different reasons.
   - The customer's line about the billing system, tagged `R9-CAUSE-UNVERIFIED`. Point at the
     timeline: the actual trigger was a stale routing rule.
   - An event the model lifted from the email, tagged `R11-EVENT-SUPPORTED`, because it is not one
     of the four events code parsed from `events.csv`.

   **Say:** "The model reported the email accurately. Code refused to promote a customer's guess to
   a fact, and refused to vouch for an event the log does not contain. Neither of those is an
   accident."
3. **Timeline** — parsed from `events.csv` by ordinary code. The model never saw that file.

**Say:** "Two unverified items are not a defect. A system that only says pass or fail will quietly
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

Or all six at once, with assertions: `scripts/demo-break-it.sh`.

**You should see** exit code 2 for the first five, and:

| Defect | Rule that catches it | Status | What it simulates |
| --- | --- | --- | --- |
| `phantom-source` | `R1-SOURCE-WHITELIST` | FAIL | a citation to a file that does not exist |
| `altered-number` | `R5-AFFECTED-CUSTOMERS` | FAIL | a number drifting from the source |
| `altered-timestamp` | `R6-TIMESTAMP` | FAIL | a plausible time that appears nowhere in evidence |
| `mislabelled-cause` | `R12-KIND-SEMANTICS` | FAIL | a guess about *why*, relabelled to slip past the cause rule |
| `spliced-quote` | `R2-QUOTE-PRESENT` | FAIL | two real fragments welded into a sentence nobody wrote |
| `unsupported-event` | `R11-EVENT-SUPPORTED` | UNVERIFIED | a plausible event the log does not contain |

Each fails on **exactly one** rule. **Say:** "Catching a problem for the wrong reason is not
catching it."

Each defect writes its own complete trio into `artifacts/break-it/<defect>/`. The clean artifacts are
read and never written — check the hash if you like.

---

## Demo 2b — the one that got past us (5 minutes, the honest one)

**Say:** "Here is a bug we shipped and then found. It is the best argument in this workshop."

```bash
dotnet run --project src/Workshop.App -- verify-only --inject-defect mislabelled-cause
```

**Say:** "The customer's email says the outage was *caused by* the billing system. That is a guess,
and `R9` exists to stop us printing it as a fact. But `R9` used to fire on the claim's *label*. So a
model that called that same sentence an `event` instead of a `cause` sailed through every rule —
real source, real quote, real everything — and landed in **Verified facts**. Zero failures. Exit
zero."

Then point at the fix: `R12-KIND-SEMANTICS` looks for causal wording in the claim's value *and* its
quotes, and does not care what the model called it.

**The point:** the trust boundary was in the right place, but it trusted one field the model
controlled. Verification you have not attacked is just verification you have not attacked yet.

---

## Demo 2c — the fabricated event (3 minutes)

```bash
dotnet run --project src/Workshop.App -- verify-only --inject-defect unsupported-event
```

**You should see exit code 0 and no failures.** Ask the room whether that is a bug.

It is not. The claim — "engineers restarted the billing service" — is cited to a real line of a real
file, so nothing about it is refutable. It is simply not in the event log, so code declines to vouch
for it: `R11-EVENT-SUPPORTED` marks it `UNVERIFIED` and it appears under **Shown but not verified**,
never in Verified facts.

**Say:** "Rejection does not have to mean going red. It means never being asserted."

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

**You should see** (~2 minutes) `OUTCOME: PASS`, with L1 smoke, L2 semantic extraction 5/5, L3 tool
contract 5/5, L4 integrated 5/5, six defects each rejected 3/3 on the intended rule, and latency
inside 30 s.

**Say:** "Five runs, not one, and every defect three times. A single passing run tells you almost
nothing about a model."

Open `artifacts/gate-report.json` and show the `provenance` block: model digest, quantization,
runtime version, settings, CPU, RAM, and the measured CPU/GPU placement. **Say:** "Numbers without
provenance are folklore. This block is why you can repeat ours."

---

## Demo 5 — provider swap (3 minutes, 120-minute version only)

Requires the organiser endpoint. **Fictional evidence only.**

```bash
MAF_ENDPOINT=<organiser> MAF_MODEL=<organiser> MAF_API_KEY=<organiser> \
  dotnet run --project src/Workshop.App -- run
```

The banner flips from `LOCAL` to `HOSTED` on its own, because the lane is read off the endpoint.

**You should see** the same three artifacts and the same verifier semantics. The ledger wording will
differ — different model, different phrasing.

**Say:** "The schema and the rules did not move. Only the thing producing claims did. That is what a
provider seam buys you — and it is why the verifier lives in a project that has no model dependency
at all."
