# 4B attendee-stage validation — 2026-08-31

Runtime: LM Studio server on local port 1234. Model: `nvidia-nemotron-3-nano-4b` (loaded locally,
2.84 GB, context 16,384). No API key or other secret is recorded here.

| Stage | Check | Outcome |
|---|---|---|
| 01 — Getting started | `dotnet build`, `dotnet test`, `smoke` | Build/test pass. `smoke` exits 2 with an empty reply, the expected CP-01 TODO baseline; it has not called a model yet. |
| 02 — Typed JSON | `dotnet build`, `dotnet test`, `smoke`, `typed` | Build/test pass. Smoke returned `WORKSHOP_OK`; typed exits 2 with the expected CP-02 TODO baseline. The contract is `SimpleCommand(action, target)` for “Mute the microphone.” |
| 03 — Gather | `dotnet build`, `dotnet test`, `query` | Build/test pass. Deterministic Gather is complete; the CP-03 QueryAgent TODO intentionally returns no typed filter until implemented. |
| 04 — Extract | `dotnet build`, `dotnet test`, `run` supported/no-evidence queries | Build/test pass. Intersection reaches `UnsupportedSelection` (the expected CP-04 TODO gate); no-evidence exits 0 and does not call the model. |
| 05 — Analyse | `dotnet build`, `dotnet test`, `run` supported/no-evidence queries | Build/test pass. Intersection reaches the expected unsupported-analysis caution outcome; no-evidence exits 0 before model work. |
| 06 — Workflow | `dotnet build`, `dotnet test`, `run` and `workflow` | Build/test pass. Both intersection commands reached `Supported`; no-evidence exited cleanly before Extract/Analyse. |

After the corpus expansion, stage 06 was repeated against the same runtime. `query --prompt "Show up to 5
intersection crashes from 2012."` returned a typed `QueryFilter` with full ISO dates, term, and cap; C# removed
the generic word `crash` and printed the validated filter. `workflow --prompt "Show up to 5 intersection crashes
from 2012."` reached `gate: Supported`. `--term` remains only a deterministic debug override; its no-evidence
branch stops before Extract and Analyse.

The primary no-evidence route was also checked: `workflow --prompt "Find cyclist crashes."` produced the
validated term `cyclist`, gathered zero records, returned `gate: NoEvidence`, and did not call Extract or Analyse.

The final stage is the model-backed recovery reference. Earlier stages intentionally demonstrate the
next incomplete boundary; their documented non-zero caution outcomes are expected, not a runtime
compatibility claim.

## Re-validation after the restructure (31 Aug 2026, later session)

Runtime: **Ollama** on local port 11434, model `nemotron-3-nano:4b` — the blessed attendee default in
`.env.example`, rather than the LM Studio lane used above. No secret is recorded here.

Re-run because the numbered-lab restructure, the 1,000-record corpus, the `--prompt`/`QueryFilter`
contract and the added clamp test all post-date the table above.

| Stage | `dotnet build` | `dotnet test` (Core / LocalModel) |
|---|---|---|
| 01 — Getting started | 0 warnings, 0 errors | 9 passed / 22 passed, 5 skipped |
| 02 — Typed JSON | 0 warnings, 0 errors | 9 passed / 22 passed, 5 skipped |
| 03 — Gather | 0 warnings, 0 errors | 10 passed / 22 passed, 5 skipped |
| 04 — Extract | 0 warnings, 0 errors | 10 passed / 22 passed, 5 skipped |
| 05 — Analyse | 0 warnings, 0 errors | 10 passed / 22 passed, 5 skipped |
| 06 — Workflow | 0 warnings, 0 errors | 11 passed / 22 passed, 5 skipped |
| `facilitator/reference/solution` | 0 warnings, 0 errors | 11 passed / 22 passed, 5 skipped |

Core grows 9 → 10 at lab 03 (the added `ModelFilterIsBoundedBeforeDeterministicGather` clamp test)
and 10 → 11 at lab 06. The 5 LocalModel skips need `WORKSHOP_LOCAL_MODEL=1`; under that flag all 27
passed against this runtime via `scripts/verify-all.sh`.

Model-backed paths, all from `workshop/06-workflow`:

- `query --prompt "Show up to 5 intersection crashes from 2012."` → model asked for term
  `"intersection crash"`; C# validation printed `"intersection"` with ISO dates and `maxResults: 5`. Exit 0.
- `run --prompt "Show up to 5 intersection crashes from 2012."` → `gathered: 5 record(s)`,
  `gate: Supported`, Extract and Analyse both returned typed JSON. Exit 0.
- `workflow --prompt "…from 2012."` → `gate: Supported`. Exit 0.
- `run` / `workflow --prompt "Find cyclist crashes."` → validated term `cyclist`, `gathered: 0 record(s)`,
  `gate: NoEvidence`, Extract and Analyse not called. Exit 0.
- `ready --prompt "…from 2012."` → `READY: model-backed supported path completed.` Exit 0.
- `gather --term intersection` (no model) → **8** records, the default cap; the corpus holds 178 matches.

Lab 03's `query` still returns the empty CP-03 baseline until the attendee implements the TODO — that
is the designed starter state, so 03's model-backed acceptance only holds after the exercise. Its
shipped acceptance is the deterministic `gather --term definitely-not-present` empty-pack check.

Corpus gate: all 8 copies of `victoria-road-crash-sample.json` share md5 `0e47ee5e2a572bd8c79ac1ee52d0870b`;
1,000 records, 1,000 unique IDs, 2012-01-11 to 2025-12-30, no fatal severities and no pedestrian summaries.
