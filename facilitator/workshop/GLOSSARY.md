# Glossary

Plain language. If a term in the room needs more than this, it is probably not needed today.

**Model** — a program that predicts text. Ours is small (4 billion parameters, ~2.8 GB) and runs on
your laptop. It is good at reading messy prose and filling in a fixed shape. It is not a database,
not a calculator, and not a source of truth.

**Prompt** — the instructions plus the input you hand the model for one job.

**Typed extraction** — asking the model to answer in a fixed structure (here: selected record IDs, a
rationale and a confidence) instead of free prose. You get something code can inspect. It guarantees
the *shape* is right. It guarantees nothing about whether the content is *true* — that is the gate's
job.

**Tool** — a function you let a model call mid-conversation. This workshop's core path deliberately
does not use one: Gather runs as plain C# before the model is ever invoked, not as a tool the model
asks for. Tool-calling only comes up in the bonus Harness/MCP comparison.

**Workflow** — the fixed order of steps your code runs: Gather, Extract, Analyse, with a code-owned
gate after each typed step. You decide the order, not the model; the model does one narrow job
inside it. Labs 03-05 run that order as plain C# you can step through in a debugger. Lab 06 expresses
the same order with the MAF Workflows API: `Executor<TIn, TOut>` nodes and conditional edges, where
omitting a gate is not a representable path. Note that `InProcessExecution.RunAsync` reports executor
failures as a `WorkflowErrorEvent` rather than throwing, so lab 06 checks for one explicitly.

**Selection** (`CrashSelection`) — Extract's typed output: record IDs chosen from the Gather pack, a
short rationale and a 0–100 confidence. A selection with an ID that isn't in the pack is just as
disqualifying as no selection at all.

**Gate** — ordinary code (`CrashWorkflow.ValidateSelection` / `ValidateAnalysis`) that checks Extract's
and Analyse's typed output before the next step runs. It returns one of: `Supported`, `NoEvidence`,
`UnsupportedSelection`, `LowConfidence`, `UnsupportedAnalysis`. Only `Supported` reaches the grounded
finding; every other outcome is a caution branch, not a failure to hide.

**LOCAL / FREE CLOUD / CONTROLLED CLOUD** — three different things this workshop never blurs:

- **LOCAL** — runs on your machine, offline once models and packages are cached.
- **FREE CLOUD** — someone else's hosted free tier. Throttled, mutable, possibly logged. Diagnostic
  only, fictional data only, never a workshop dependency.
- **CONTROLLED CLOUD** — an organiser-owned, OpenAI-compatible deployment used as a recovery lane,
  reached with a key and three environment variables. No code change.
