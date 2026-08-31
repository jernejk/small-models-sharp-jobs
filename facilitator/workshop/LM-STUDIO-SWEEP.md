> Historical (pre-29-Aug pivot): describes the earlier incident-pack build. Current path: AGENDA-85.md.

# LM Studio model sweep — 24 August 2026

Every locally available LM Studio model run through the full gate matrix, smallest to largest. This
closes the **LM Studio parity** item that [CLAIMS-AND-LIMITS.md](../archive/retired-pre-restructure-material/CLAIMS-AND-LIMITS.md) listed as
UNVERIFIED — on one machine, which is not the same as on every machine.

## Machine and settings

```text
MacBook Pro (Mac16,5) · Apple M4 Max · 64 GB unified memory · macOS (Darwin 27.0.0) · arm64
.NET SDK 10.0.302 · LM Studio CLI 0.12.11, server on http://localhost:1234/v1
non-streaming · temperature 0 · reasoning effort none · per-call ceiling 90 s
gates --repeat 5, plus six seeded defects × 3 attempts
```

**This machine is roughly 4× the reference laptop.** Nemotron measures 5.0 s here against 19.6–22 s
on the WSL2 reference machine in [REHEARSAL.md](REHEARSAL.md). Do not quote these latencies as what
an attendee will see.

**Context length is not what was asked for.** Every model loaded at **262144** tokens despite
`lms load --context-length 8192`. That is LM Studio's choice, not the application's, and it is a
plausible contributor to the slowest result below. Not investigated.

## Results

Ordered by size on disk. "claims / pass / fail / unver" is the run output; the Ollama reference
produces **7 / 30 / 0 / 2**.

| Model | Size | Params | L1 | L2 | L3 | L4 | L5 | Median | Worst | claims/pass/fail/unver | Verdict |
| --- | ---: | ---: | --- | --- | --- | --- | --- | ---: | ---: | --- | --- |
| `nvidia-nemotron-3-nano-4b` | 2.84 GB | 4B | PASS | 5/5 | 5/5 | 5/5 | 18/18 | **5.0 s** | 6.0 s | 7/30/0/2 | **PASS** — matches the Ollama reference exactly |
| `google/gemma-4-12b` | 6.77 GB | 12B | PASS | **0/5** | **0/5** | 5/5 | 18/18 | **94.6 s** | 122.2 s | 4/20/0/0 | **FAIL** — never calls the tool, 20× slower |
| `gemma-4-12b-it-qat` | 6.98 GB | 12B | PASS | 5/5 | 5/5 | 5/5 | 18/18 | 11.1 s | 11.6 s | 7/29/0/3 | PASS |
| `prism-ml/bonsai-27b` | 8.52 GB | 27B | PASS | 5/5 | 5/5 | 5/5 | 18/18 | 11.6 s | 14.1 s | 5/23/0/1 | PASS, but thin — drops two claims |
| `google/gemma-4-26b-a4b-qat` | 15.64 GB | 26B (4B active) | PASS | 5/5 | 5/5 | 5/5 | 18/18 | **4.8 s** | 7.9 s | 7/30/0/2 | **PASS** — fastest, matches the reference |
| `qwen/qwen3.8-27b` | 16.08 GB | 27B | PASS | 5/5 | 5/5 | 5/5 | 18/18 | 18.2 s | 18.9 s | 6–7 / 26–29 / 0 / 2–3 | PASS, but **not reproducible** |

## What the sweep actually shows

**Size does not predict fitness, in either direction.** The fastest model in the set is the largest
file on disk — a 15.64 GB mixture-of-experts with only 4B parameters active. The slowest, by a factor
of twenty, is a 6.77 GB dense 12B. Bytes on disk told you nothing; active parameters and the build
told you almost everything.

**The build matters more than the family.** `gemma-4-12b-it-qat` and `google/gemma-4-12b` are the
same model family at the same parameter count. The QAT build passes every gate at 11.1 s. The other
build never successfully calls the tool across five runs and takes 94.6 s to fail. If you take one
slide from this sweep, take that one.

**Only two models reproduce the reference output.** `nemotron-3-nano-4b` and `gemma-4-26b-a4b-qat`
both produce 7 claims, 30 passed, 0 failed, 2 unverified. The others pass the gates while extracting
less: `bonsai-27b` finds 5 claims where the reference finds 7. Passing L2 means the *required* kinds
are present and correct — it does not mean the model found everything.

**`qwen3.8-27b` is the only model that varied between runs.** Run 1 gave 6 claims / 26 passed; runs
2–5 gave 7 / 29. Every other model produced byte-identical output five times at temperature 0.
Reproducibility is a model property, not a given, and it is worth naming out loud in the workshop.

## Two cautions about the gates themselves

**L5 does not discriminate between models.** All six scored 18/18, including the one that fails
everything else, because the seeded defects are verified against a hand-built ledger and never touch
the model. L5 proves the *verifier* works. Do not quote it as evidence about a model.

**L4 alone is a weak signal.** `google/gemma-4-12b` passed L4 5/5 while failing L2 and L3 0/5. L4
requires zero verification *failures*, and a model that extracts only four claims and never calls the
tool still produces a ledger with nothing failing in it. The gate set as a whole catches it — L3
does the work. A green L4 on its own means very little.

## Recommendations

1. **Keep `nemotron-3-nano-4b` blessed.** Smallest download, matches the Ollama reference exactly,
   fastest of the models that do, and it is the one the attendee prefetch already pulls.
2. **LM Studio now works end to end** — but only after the provenance fix below. Ollama stays the
   blessed runtime; LM Studio is a working alternative rather than a second blessed path, because
   this is one machine.
3. **If an attendee is already on LM Studio with a gemma**, `gemma-4-12b-it-qat` is a sound fallback.
4. **Steer anyone away from `google/gemma-4-12b`** for this workshop. It fails the tool contract.

## The bug this sweep found

Against LM Studio, `gates` passed every gate and then **exited 3 with `KeyNotFoundException`**,
writing no gate report. LM Studio answers unknown paths with **HTTP 200 and a valid JSON error body**
rather than a 404, so `Provenance.CaptureAsync` parsed the response successfully and then threw on
`GetProperty("version")` — a type the surrounding catch did not cover.

Fixed by reading the version through the existing safe accessor, with three regression tests
(`ProvenanceTests`). Provenance now degrades to `unavailable` on a non-Ollama runtime instead of
taking the run down. The whole sweep above ran on the pre-fix binary, which is why every run exited
3; re-verified after the fix with `OUTCOME: PASS` and a written gate report.

Two further bugs surfaced in `scripts/rehearse-60.sh`, both macOS-only and both now fixed: `grep -oP`
is GNU-only and silently reported every checkpoint as zero, and `dotnet run --no-build` after
`dotnet test` failed to load `Workshop.Core` because the deps file no longer declared it. The script
had therefore never worked on a Mac. It now reports `REHEARSE_60: PASS` there.

## Not established here

- Anything about a machine that is not this one. One data point.
- Attendee-representative latency. This hardware is far above the reference laptop.
- Behaviour at the context length the application intended, rather than 262144.
- Any hosted lane. Still no live hosted call has been made.
- Whether `bonsai-27b`'s thinner extraction would degrade further on a longer evidence pack.
