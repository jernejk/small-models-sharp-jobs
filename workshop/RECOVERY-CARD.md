# Recovery card

Print this. One page. Hand it to anyone who is stuck.

---

## Stuck for five minutes? Stop debugging. Use this card.

### 1. Is the runtime up?

```bash
ollama serve            # leave it running in its own terminal
ollama list             # nemotron-3-nano:4b should be listed
```

```bash
dotnet run --project src/Workshop.App -- smoke
```

**Want:** `smoke: PASS [JACKDAW_OK]`

---

### 2. Code problem? Copy the answer.

```bash
cp ../solution/src/Workshop.Core/EvidenceStore.cs   src/Workshop.Core/    # TODO 1
cp ../solution/src/Workshop.App/IncidentPipeline.cs src/Workshop.App/     # TODO 2 + 3
cp ../solution/src/Workshop.Core/Verifier.cs        src/Workshop.Core/    # TODO 4
```

Not cheating. Falling silently behind is the only failure here.

---

### 3. Model won't run? Switch lanes. No code change.

```bash
export MAF_ENDPOINT=<facilitator gives you this>
export MAF_MODEL=<facilitator gives you this>
export MAF_API_KEY=<facilitator gives you this, if any>

dotnet run --project src/Workshop.App -- run
```

Same schema, same verifier, same three artifacts. Different model, so different wording — the
ledger will not match your neighbour's exactly. That is expected.

> **Fictional evidence only.** Never send real incident data, private code or a credential to any
> endpoint. Everything in `evidence-pack/` is invented for this workshop.

---

### 4. No model at all? Do the deterministic half.

Everything except extraction runs with no model whatsoever:

```bash
dotnet test                                                        # 63 tests
dotnet run --project src/Workshop.App -- verify-only               # verify + render an existing ledger
dotnet run --project src/Workshop.App -- verify-only --inject-defect altered-number
```

A `claim-ledger.json` from any working machine is enough to do TODO 4 and the whole break-it
exercise. Ask a neighbour for theirs.

---

## Exit codes

| Code | Meaning |
| --- | --- |
| 0 | verification passed |
| 2 | verification failed — a claim did not hold up (this is the tool working) |
| 3 | pipeline error — bad path, model unreachable |
| 4 | gate failed |

## The three commands that matter

```bash
dotnet test
dotnet run --project src/Workshop.App -- run
dotnet run --project src/Workshop.App -- verify-only --inject-defect altered-number
```
