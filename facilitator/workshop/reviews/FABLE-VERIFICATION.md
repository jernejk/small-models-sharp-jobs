# Fable verification lane — independent reproduction

**Status: run. Verdict recorded below as received.**

Fable ran the verification lane against the implementation, independently reproducing the
measurements rather than reading them. This file records what it reproduced and what it found, so
the findings survive independently of whether Opus agreed with them.

## Independently reproduced

Fable rebuilt from scratch and re-measured rather than trusting the repo's own numbers:

| Reproduced | Result |
| --- | --- |
| Fresh build from a clean tree | succeeded |
| Deterministic test suite | 63/63 (pre-correction build) |
| Gate matrix, 5 repetitions | 5/5, median 24.3 s, worst 24.6 s |
| Seeded defects fail on their intended rule | confirmed for the three defects that existed |
| Renderer byte-stability | identical bytes for identical input |
| Model placement | 30% CPU / 70% GPU |
| Offline Tier A | blocked-egress proof confirmed |

Those latency figures describe the **pre-correction** build. The current build measures 19–22 s;
see [CLAIMS-AND-LIMITS.md](../../archive/retired-pre-restructure-material/CLAIMS-AND-LIMITS.md). Fable's reproduction is not superseded by that
— it measured a different build, and both are recorded.

## Findings

| # | Finding | Opus disposition |
| --- | --- | --- |
| F1 | `dotnet test --filter EvidenceStoreTests` returns **14**, not the documented 31 | **Accepted and fixed.** 14 is the filtered count; 80 is the whole-suite total after TODO 1. Both now stated, in every doc that carried the cue |
| F2 | `MAF_AUTH=azure-cli` is a dead flag — nothing reads it | **Accepted and fixed.** Flag removed. See [OPUS-DISPOSITION.md](OPUS-DISPOSITION.md) must-fix 4 for why the credential was not implemented instead |
| F3 | No git repository, so clone/reset instructions are invalid; scripts not executable; artifacts ignored | **Accepted and fixed.** Local repository initialised, scripts committed `100755`, clean clone and `git archive` both proven to build and test, `git checkout artifacts/` replaced with `scripts/reset-workshop.sh` |
| F4 | Latency prose is inconsistent between documents, and placement is absent from the gate report | **Accepted and fixed.** All latency prose now derives from the two gate runs of the current build, with the pre-correction figures explicitly quarantined. Placement is captured into `gate-report.json` provenance from Ollama, not asserted |
| F5 | Event causal semantics are kind-dependent | **Accepted; this is the most serious finding in either lane.** Independently raised by Sol as must-fix 1 and reproduced as a working evasion before fixing. See below |
| F6 | Optional Qwen setup is absent although the 120-minute agenda references it | **Accepted and fixed.** `prefetch.sh` pulls it under `WORKSHOP_PREFETCH_QWEN=1`, and it is listed as UNVERIFIED |

## F5, reproduced as a working attack before it was fixed

Fable's finding and Sol's must-fix 1 are the same defect. It was reproduced concretely rather than
accepted on description — a hand-built ledger labelling the customer's causal sentence as kind
`event`:

```text
before:  22 passed, 0 failed, 0 unverified, exit 0
         "the outage was caused by the new billing system" -> Verified facts
after:   R12-KIND-SEMANTICS FAIL + R9-CAUSE-UNVERIFIED, kept out of Verified facts
```

A second evasion was found while fixing the first, and is recorded because it was not in either
review: a quote spliced from two different source lines passed `R2-QUOTE-PRESENT`, because
normalization flattened the whole file into one string. Both are now seeded defects.

## Lane status

| Lane | Role | Status |
| --- | --- | --- |
| Fable | planning, then verification | plan constraints received; **verification run, findings above, all six accepted** |
| Opus 5 | implementation, then correction cycle | complete; dispositions in [OPUS-DISPOSITION.md](OPUS-DISPOSITION.md) |
| Sol high | independent third-party review | **run — verdict revise / PARTIAL**, recorded in [SOL-REVIEW.md](SOL-REVIEW.md) |
