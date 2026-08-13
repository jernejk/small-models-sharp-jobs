# Reference run

The measured artifacts every number in this repo comes from. Committed on purpose: a readiness
claim with no artifact behind it is a story.

| File | What it is |
| --- | --- |
| `claim-ledger.json` | the ledger from one clean run — the model proposed the claims, code assembled the file |
| `verification.json` | what deterministic code decided about it: 30 passed, 0 failed, 2 unverified |
| `incident-brief.md` | the rendered brief, built only from claims that passed |
| `gate-report.json` | first gate matrix, 5 repetitions — median 20.9 s, worst 22.0 s |
| `gate-report-2.json` | second gate matrix, 5 repetitions — median 19.6 s, worst 20.0 s |

Both gate reports carry a `provenance` block: model digest, quantization, runtime version, the
settings actually used, the machine, and the measured CPU/GPU placement. Read that before repeating
a number.

`scripts/check-distribution.sh` verifies a clean clone against `claim-ledger.json` here, so this
directory is load-bearing, not decorative. `scripts/reset-workshop.sh` never touches it.
