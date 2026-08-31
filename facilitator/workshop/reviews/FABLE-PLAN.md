# Fable planning lane — constraints as received

**Status: this is not Fable's full plan document.**

The Fable planning lane ran read-only before implementation. What reached the implementation lane
was a distilled constraint list, not Fable's complete prose plan. That list is reproduced verbatim
below. It is recorded this way so nobody later mistakes an Opus paraphrase for Fable's own words,
and so the gap is visible rather than papered over.

If the full Fable plan document is recoverable, it should be added here verbatim and this note
replaced.

## Received verbatim

> FABLE PLANNING LANE COMPLETED READ-ONLY. Treat these as required unless evidence refutes them:
>
> - Live inventory confirmed dotnet 10.0.302, Ollama 0.32.9, Nemotron and Qwen models present,
>   cached MAF 1.17.0 packages, no existing target workspace.
> - Extract per source with expected-facts.json hidden from the model; merge deterministically.
> - Fixed claim kinds: incident_id, severity, affected_customers, timestamp, duration, event, cause.
> - Quote verification: NFC normalize, collapse whitespace, trim, then exact substring.
> - verification.json needs claimId, ruleId, PASS/FAIL/UNVERIFIED, detail; gates assert intended rule IDs.
> - Renderer must be byte-stable, render PASS with citations, show UNVERIFIED visibly, count excluded
>   FAIL, and return nonzero on FAIL.
> - Generate starter from canonical solution TODO regions and verify both compile plus no drift
>   outside TODO regions.
> - Four-source projected warm latency is close to 30s; trim fixture tokens before raising context.
> - Offline proof is Tier A automated no-nonloopback-egress plus clean local-cache restore; Tier B
>   physical Wi-Fi disconnect remains human rehearsal.
> - Hard gates D1-D7 and L1-L8 from the mission: typed 5/5, tools 5/5, integrated 5/5, seeded defects
>   3/3 with intended rule IDs, warm every run <=30s, >90s hard fail.
> - Bless Ollama unless LM Studio passes the exact same full-path gate. Hosted/free lanes are optional
>   and never block core.
> - Human non-author rehearsal cannot be fabricated; document it pending if unavailable.
> - Work only on Jackdaw target workspace over SSH. Do not publish, email, push, or handle raw secrets.

Opus's disposition of each constraint, with evidence, is in
[OPUS-DISPOSITION.md](OPUS-DISPOSITION.md).

## Lane status

| Lane | Role | Status |
| --- | --- | --- |
| Fable | planning, then verification | plan constraints received; verification **run** — see [FABLE-VERIFICATION.md](FABLE-VERIFICATION.md) |
| Opus 5 | implementation, then correction cycle | complete — see [OPUS-DISPOSITION.md](OPUS-DISPOSITION.md) |
| Sol high | independent third-party review | **run**, verdict `revise` / PARTIAL — see [SOL-REVIEW.md](SOL-REVIEW.md) |

Constraint 10 of the plan asked for "seeded defects 3/3 with intended rule IDs". The implementation
originally read that as three defects checked once each. Sol's review read it as each defect
rejected three times. The stricter reading is now implemented: six defects, three attempts each.
