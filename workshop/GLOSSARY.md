# Glossary

Plain language. If a term in the room needs more than this, it is probably not needed today.

**Model** — a program that predicts text. Ours is small (4 billion parameters, ~2.8 GB) and runs on
your laptop. It is good at reading messy prose and filling in a fixed shape. It is not a database,
not a calculator, and not a source of truth.

**Prompt** — the instructions plus the input you hand the model for one job.

**Typed extraction** — asking the model to answer in a fixed structure (here: a list of claims, each
with a kind, a value and a quote) instead of free prose. You get something code can inspect. It
guarantees the *shape* is right. It guarantees nothing about whether the content is *true* — that is
the verifier's job.

**Tool** — a function you let the model call. Ours is `read_evidence`, which returns one file from
the evidence pack. The model can ask for a file; it cannot reach anything else on your disk, because
the tool only accepts names on a whitelist.

**Workflow** — the fixed order of steps your code runs: fetch evidence, extract claims, verify,
render. You decide the order, not the model. The model does one narrow job inside it.

**Claim ledger** (`claim-ledger.json`) — the model's output as data. Every claim carries a kind, a
value, the source it came from and the exact quote that supports it. A claim without a quote is
just an assertion.

**Verifier** (`verification.json`) — ordinary code that checks each claim against the evidence and
against facts parsed independently from the source files. It emits one of three results per rule:

- `PASS` — the check ran and the claim held up.
- `FAIL` — the check ran and the claim did not hold up. Excluded from the brief.
- `UNVERIFIED` — no deterministic check can settle this. Shown in the brief, clearly marked, never
  presented as fact.

`UNVERIFIED` is the honest third answer. A system with only pass and fail will quietly promote
things it cannot actually check.

**Renderer** — code that writes `incident-brief.md` from claims that passed. Same input, same bytes,
every time. The model does not write this file.

**Ground truth / source parsing** — facts the code reads straight out of the evidence with regular
expressions and a CSV parse. Because the model never touches this path, comparing the two is a real
check rather than a model grading itself.

**Seeded defect** — a deliberate corruption we inject to prove the verifier bites: a phantom source,
an altered number, an altered timestamp.

**LOCAL / FREE CLOUD / CONTROLLED CLOUD** — three different things this workshop never blurs:

- **LOCAL** — runs on your machine, offline once models and packages are cached.
- **FREE CLOUD** — someone else's hosted free tier. Throttled, mutable, possibly logged. Diagnostic
  only, fictional data only, never a workshop dependency.
- **CONTROLLED CLOUD** — an organiser-owned Azure OpenAI deployment used as a recovery lane.
