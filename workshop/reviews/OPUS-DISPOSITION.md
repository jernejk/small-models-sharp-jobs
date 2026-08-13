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

## Not done

- Fable implementation verification — **not yet run**.
- Sol high independent review — **not yet run**.

Both are scheduled after this implementation lane and are required before the workshop can be called
complete.
