# Attendee guide

You will complete an evidence lookup tool, connect a typed extraction step, add one deterministic
verification rule, then run the application to create a claim ledger, verification report and cited
incident brief.

Work in `starter/`. The finished code is in `solution/` if you get stuck — using it is not cheating,
falling behind quietly is the only real failure.

```bash
cd starter
dotnet test    # 10 passed, 53 failed. That is the correct starting point.
```

## What you are building

A fictional incident (`INC-042`) arrives as four files: a status page, a customer email, an event
log and a runbook. Your application must turn that pile into a brief a manager could act on —
without letting the model state anything it cannot back up.

```text
read_evidence tool  ->  typed extraction  ->  claim ledger
                    ->  verification      ->  incident-brief.md
```

The model does exactly one job: read prose, return typed claims with quotes. Everything that decides
what is *true* is ordinary C# you can step through in a debugger.

---

## TODO 1 — constrain the evidence tool

`src/Workshop.Core/EvidenceStore.cs`

The model will be able to call this. Make it read only files on `Whitelist`, and reject everything
else: unknown names, empty names, and anything trying to climb out of the directory with `..` or an
absolute path.

Return `false` with a useful `error`; do not throw.

> **Why a whitelist and not a path check?** A path check asks "where did this end up?" A whitelist
> asks "did I ever agree to serve this?" The second question is much harder to trick. `expected-facts.json`
> sits in the same folder and is deliberately *not* on the list — the model must never read the answer key.

```bash
dotnet test --filter EvidenceStoreTests
```

**Expect:** 31 passed. Traversal, unknown IDs, and the answer-key file are all refused.

---

## TODO 2 — register the tool

`src/Workshop.App/IncidentPipeline.cs`

Hand `EvidenceToolHost.ReadEvidence` to the agent as a tool named `read_evidence`, using
`AIFunctionFactory.Create`. The name and description are what the model sees — they are part of
the prompt, so make them accurate.

**Expect:** builds. You will not see the effect until TODO 3.

---

## TODO 3 — connect typed extraction

`src/Workshop.App/IncidentPipeline.cs`

Call `agent.RunAsync<SourceExtraction>(prompt)` and turn each returned claim into an
`ExtractedClaim`, attaching the `sourceId` you already know.

> **Why does the extraction agent have no tools?** On the reference model, asking for structured
> output *and* offering tools in the same call returns `{"claims": []}` in about 1.4 seconds and the
> tool is never invoked. So the pipeline splits them: one agent call fetches with the tool, a second
> extracts typed claims from what came back. Small models make you separate concerns you could have
> been sloppy about with a large one.

**Expect:** builds.

---

## TODO 4 — write a verification rule

`src/Workshop.Core/Verifier.cs`

Rule `R2-QUOTE-PRESENT`: the quote a claim cites must actually occur in the source it names. Use
`TextNormalization.ContainsQuote`, which normalizes Unicode and collapses whitespace on both sides
first, so a quote that only differs by line wrapping still matches.

Return `Pass` or `Fail` with a detail message naming the source.

```bash
dotnet test
```

**Expect:** 63 passed, 0 failed.

---

## Run it

```bash
dotnet run --project src/Workshop.App -- run
```

Roughly 25 seconds. Then open the three files in `artifacts/`:

**`claim-ledger.json`** — what the model said. Every claim has a source and an exact quote.

**`verification.json`** — what code decided. Look for the one `UNVERIFIED`.

**`incident-brief.md`** — the brief. Two things to notice:

1. The customer's line *"the outage was caused by the new billing system"* is **not** in Verified
   facts. It is under **Shown but not verified**, marked `R9-CAUSE-UNVERIFIED`. The evidence log
   says the actual trigger was a stale routing rule. The model faithfully extracted what the email
   claimed; the verifier refused to promote a customer's guess to a fact.
2. The timeline was parsed from `events.csv` by code. The model never saw that file — sending it
   would cost about ten seconds for a worse answer than a CSV parser gives for free.

## Break it

```bash
dotnet run --project src/Workshop.App -- verify-only --inject-defect altered-number
```

Exit code 2. The claim is gone from the brief and named under **Excluded by verification** with the
rule that caught it. Try `phantom-source` and `altered-timestamp` too.

Now break it the other way: open `claim-ledger.json`, change a quote to something that is *not* in
the source, and run `verify-only`. Watch `R2-QUOTE-PRESENT` — the rule you wrote — catch it.

## What to take home

- Give a small model one narrow job with a fixed output shape.
- Give it a tool that can only reach what you whitelisted.
- Let ordinary code decide what is true, and let it say `UNVERIFIED` when it cannot tell.
- Render the final artifact deterministically, so the output is reviewable and diffable.

None of this requires a frontier model. It requires being specific about who is responsible for what.
