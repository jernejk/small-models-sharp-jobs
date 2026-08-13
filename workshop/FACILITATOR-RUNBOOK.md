# Facilitator runbook

Attendees complete an evidence lookup tool, connect a typed extraction step, add one deterministic
verification rule, then run the application to create a claim ledger, verification report and cited
incident brief.

## The five-minute rule

**No attendee debugs their environment for more than five minutes.**

At five minutes, move them to the recovery lane and keep going. Set a visible timer if you have to.
The workshop teaches an architecture, not an installation. An attendee who spends 25 minutes on a
proxy error learns nothing and takes a facilitator with them.

Escalation order, in order:

1. **Restart the runtime.** `ollama serve` in a fresh terminal. Fixes more than it should.
2. **Copy from `solution/`.** For a code problem, hand them the file and move on. They can read it
   later; they cannot re-attend the workshop.
3. **Recovery lane.** Two environment variables, no code change. See [RECOVERY-CARD.md](RECOVERY-CARD.md).
4. **Pair them up.** Last resort and genuinely fine — two people at one working laptop beats one
   person at two broken ones.

## Before the room opens

```bash
python3 scripts/generate-starter.py --check    # starter/ and solution/ match src/
dotnet test                                    # 63 passed
dotnet run --project src/Workshop.App -- gates --repeat 5
```

The gate run takes about 2 minutes and prints `OUTCOME: PASS`. If it does not, you are presenting a
broken configuration — fix it or switch to the recovery lane for the whole room.

Also do:

- Load the model once so it is warm: `dotnet run --project src/Workshop.App -- smoke`.
- Have `solution/` open in a second window.
- Have the recovery endpoint details on a slide, not in your head.
- Print or display [RECOVERY-CARD.md](RECOVERY-CARD.md).

## Known failure modes

| Symptom | Cause | Fix |
| --- | --- | --- |
| Empty response, ~57 s | Reasoning left on. The runtime auto-enables thinking when `reasoning_effort` is absent. | `ReasoningEffort.None` — already set in `CreateAgent`. If someone deleted it, restore it. |
| `{"claims": []}` in ~1.4 s | Tools and structured output in the same call. | They are separate agent runs on purpose. Do not "simplify" this. |
| `connection refused` on 11434 | Ollama not running. | `ollama serve` |
| `model not found` | Model not pulled. | `ollama pull nemotron-3-nano:4b` (2.8 GB — recovery lane if on venue Wi-Fi) |
| Restore fails offline | Packages never cached. | Needs network once. Recovery lane today. |
| Run takes 60 s+ | CPU-only or a loaded machine. | Fine for one run. If it repeats, recovery lane. |
| `dotnet test` fails in `starter/` | Expected before the TODOs. | 10 passed / 53 failed is the correct start. |
| Verification fails on a clean run | Usually a hand-edited evidence pack. | `git checkout evidence-pack/` |

## Reading the room

- **Minute 17 (60-min) / 45 (120-min)** — ask for a show of hands on 31 passing. Below two-thirds,
  walk TODO 1 on the screen rather than waiting.
- **The green moment** is `dotnet test` → 63 passed. Call it out. Let people enjoy it.
- **The teaching moment** is `incident-brief.md`. Read the *Shown but not verified* section aloud.
  If you land nothing else, land this: the model reported the customer's claim, and code refused to
  promote it to a fact.
- **The best five minutes** are attendees hand-editing `claim-ledger.json` to beat their own rule.
  Protect that time.

## Questions you will be asked

**"Why not just use a bigger model?"** You can. The architecture is the point — verification is what
makes any model's output reviewable. A bigger model fails less often, in ways you notice less often.

**"Isn't the verifier just more code to maintain?"** Yes. That is the trade: a fixed cost you can
test, instead of a variable cost you cannot predict.

**"Could the model write the verifier?"** It could draft it. It must not be the thing that decides
whether its own output passed — that is circular. We never ask a model to grade its own factual
correctness.

**"Does typed output mean the answer is right?"** No, and this is the most common misconception in
the room. Typed output guarantees *shape*. The verifier is what addresses *content*. JSON grammar is
not truth.

**"Will this work on my laptop?"** Ours ran on a 4 GB laptop GPU at 70% offload in under 25 seconds.
We have not tested every machine and we do not claim it runs everywhere — that is what the recovery
lane is for.

## After the session

- `git checkout evidence-pack/ artifacts/` to reset for the next run.
- Note actual times against [AGENDA-60.md](AGENDA-60.md) checkpoints and update
  [REHEARSAL.md](REHEARSAL.md) with what really happened, including anything that broke.
