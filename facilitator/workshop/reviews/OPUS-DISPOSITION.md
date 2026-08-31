# Opus disposition of the Fable planning constraints

One row per constraint received in [FABLE-PLAN.md](FABLE-PLAN.md). "Modified" means implemented
differently, with the evidence that drove the change. Fable did not approve these modifications —
they are Opus decisions, recorded so the verification lane can accept or reject them.

| # | Constraint | Disposition | Evidence |
| --- | --- | --- | --- |
| 1 | Live inventory (dotnet 10.0.302, Ollama 0.32.9, both models, MAF 1.17.0 cached, no target workspace) | **Accepted** — independently reconfirmed before any writes | `dotnet --version`, `ollama list`, `ls ~/.nuget/packages/microsoft.agents.ai`, target directory absent |
| 2 | Extract per source; `expected-facts.json` hidden from the model; merge deterministically | **Accepted** | File exists on disk but is off the tool whitelist; `RejectsRealFileThatIsNotWhitelisted` proves existence is not sufficient. `LedgerBuilder` merge is order-independent (`ClaimIdsDoNotDependOnExtractionOrder`) |
| 3 | Fixed claim kinds | **Accepted as a string set, rejected as a C# `enum`** | A probe with an `enum` returned `Severity` for the value `7` and `Duration` for timestamps. Strings plus an allowed-value list in the prompt returned all seven kinds correctly. Kind validity is enforced by `R10-KNOWN-KIND` |
| 4 | Quote verification: NFC, collapse whitespace, trim, exact substring | **Accepted** | `TextNormalization.Normalize`; `QuoteSurvivesLineWrappingAndUnicodeNormalization` |
| 5 | `verification.json` carries claimId, ruleId, PASS/FAIL/UNVERIFIED, detail; gates assert intended rule IDs | **Accepted** | `EveryResultCarriesClaimIdRuleIdStatusAndDetail`; `SeededDefectTests.DefectFailsForTheIntendedRule`; gate output names actual vs expected rule per defect |
| 6 | Renderer byte-stable, PASS with citations, UNVERIFIED visible, excluded FAIL counted, non-zero exit on FAIL | **Accepted** | `IdenticalInputProducesIdenticalBytes`, `VerifiedClaimsAppearWithCitations`, `UnverifiedClaimIsVisibleAndNotInVerifiedFacts`, `FailuresAreCountedAndNamedRatherThanDropped`; exit code 2 observed |
| 7 | Generate starter from canonical TODO regions; both compile; no drift outside regions | **Accepted, extended** | `generate-starter.py` also generates `solution/`, so no tree is hand-maintained. Both compile; `--check` reports no drift; starter is red (10/53) and solution green (63/63) |
| 8 | Four-source warm latency near 30 s — trim fixture tokens before raising context | **Modified** | Measured four-source projection was **39.8 s**, over budget rather than near it. Instead of trimming prose, the model now reads only the two prose files; `events.csv` is parsed by code and `runbook.md` is policy. Result: **23.9 s median, 24.8 s worst**. This is the workshop's own thesis applied to itself, and it makes the timeline more accurate, not less |
| 9 | Offline proof: Tier A automated (no non-loopback egress + clean local-cache restore); Tier B human | **Accepted** | `scripts/offline-proof.sh` rejects all non-loopback traffic for the workshop user via iptables owner match, proves external HTTPS is unreachable while loopback still serves the model, restores with all remote NuGet sources cleared, and produces all three artifacts → `OFFLINE_PROOF: PASS`. Tier B documented as pending human rehearsal |
| 10 | Hard gates: typed 5/5, tools 5/5, integrated 5/5, defects 3/3 on intended rule, warm ≤30 s, >90 s hard fail | **Accepted; numbering changed** | Implemented as L1–L6 plus L6b in `Workshop.App gates`, and the deterministic D-gates as the 63-test suite. All substance is present and passing; only the labels differ from "D1–D7 / L1–L8" |
| 11 | Bless Ollama unless LM Studio passes the same full-path gate | **Accepted** | Ollama blessed throughout. LM Studio appears only as a documented compatibility target, in `.env.example` (commented) and under UNVERIFIED |
| 12 | Human non-author rehearsal cannot be fabricated; document pending | **Accepted** | `scripts/rehearse-60.sh` measures machine time only (**33.6 s**) and says so. Human rehearsal is marked pending in `REHEARSAL.md` and `CLAIMS-AND-LIMITS.md` |
| 13 | Work only on the Jackdaw target workspace; no publish, push, email or raw secrets | **Accepted** | All work under `~/work/global-ai-construct-offline-workshop`. The readiness pack was read, never written. No network egress beyond reading two public documentation pages for primary-source citations. No credentials handled; `.env.example` contains placeholders only |

## Findings the plan did not anticipate

**Structured output and tool-calling cannot be combined in one agent run on this model and runtime.**
A single call offering both returns `{"claims": []}` in about 1.4 seconds and never invokes the
tool — no error, no warning. Verified three ways in isolation: typed-without-tools works,
untyped-with-tools works, the combination silently returns nothing.

This forced the central architectural change: the pipeline uses **two agent calls** — one tool-enabled
gather, one tool-free typed extraction over the text the tool returned. Constraint 8's latency budget
is met with this shape in place. It is now a documented teaching point (DEMOS.md demo 3), because it
is exactly the kind of constraint that makes small-model work honest.

**Agent instances carry state across runs.** Reusing one extraction agent across sources leaked
`status.txt` facts into the `customer-email.txt` extraction — it emitted `incident_id = "SEV-2"` and
`severity = "7"`, cited to the email. A fresh agent per source removed it. Failure count went 7 → 0.

**Evidence content shapes gate reliability.** An earlier `customer-email.txt` said "Seven of our
sites" and "this morning", which the model faithfully extracted as `affected_customers = "Seven of
our sites"` and `timestamp = "this morning"` — both correctly failing their rules and both breaking
the clean-run gate. The email was rewritten to carry only impact and an unsupported cause. The
conflicting-number scenario now lives in the seeded defects, where it is deliberate rather than
incidental.

## Correction cycle — 14 August 2026

Both review lanes have now run. Fable verified independently and returned six findings; Sol returned
`revise` / PARTIAL with ten must-fixes and seven additional notes. Every one was corrected in a
single bounded cycle.

- Fable's findings and their dispositions: [FABLE-VERIFICATION.md](FABLE-VERIFICATION.md)
- Sol's must-fixes and their dispositions: [SOL-REVIEW.md](SOL-REVIEW.md)

**What changed architecturally**, beyond the itemised fixes:

1. **The trust boundary moved from the label to the meaning.** `R9` used to key off `claim.Kind`,
   a field the model chooses. A claim's kind is now evidence, not authority: `ClaimSemantics`
   detects causal wording in the value and the quotes, `R12-KIND-SEMANTICS` fails a claim whose
   label contradicts its content, and `R11-EVENT-SUPPORTED` grades event text against phrases
   parsed from `events.csv`. Rules that depend on a field the model controls are not rules.
2. **`R2` is scoped to one physical line**, closing a splice that both review lanes missed.
3. **Rejection is no longer synonymous with failure.** `UNVERIFIED` counts as rejecting a defect,
   because the property that matters is "never reached Verified facts", not "went red". The gate
   and the tests assert that property directly, per defect, three times each.
4. **Provenance is captured, not asserted.** Model digest, runtime version, settings, hardware and
   CPU/GPU placement are read from the runtime into every gate report.
5. **Every model call is bounded.** The 90 s ceiling is enforced by cancellation rather than
   observed after the fact.
6. **The tree became a repository.** Clean clone and `git archive` are both proven to build and
   test; scripts are executable in the index.

**Two claims in this repo were found to be wrong by re-reading primary sources**, not by anything
failing: Ollama's OpenAI-compatible endpoint does not support `tool_choice`, and the cited page
never stated that thinking is auto-enabled when `reasoning_effort` is absent. Corrected in
[CLAIMS-AND-LIMITS.md](../../archive/retired-pre-restructure-material/CLAIMS-AND-LIMITS.md).

## Still not done

- **A non-author human rehearsal of the 60-minute path.** Cannot be fabricated. The agenda remains
  credible rather than proven until someone who did not write this sits it end to end.
- **Any live hosted call.** The lane is configuration-only and unit-tested; nothing has been sent.
- **Tier B offline** — physically disconnecting the radio.
- **LM Studio parity**, and any machine other than the reference machine.
