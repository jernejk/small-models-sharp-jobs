# Fable execution plan — 29 August 2026

## Evidence and intent

The existing checkout already demonstrates an evidence tool, separate typed extraction, deterministic
verification and a local-model seam. The agreed redesign narrows the workshop to an 85-minute
offline flow: deterministic Gather, typed Extract, high-effort Analyse, and visible code-owned
branches. Its key compatibility claim is supported by the recorded spike: Azure works with combined
tools plus typed output, while the tested local MAF route does not reliably do so.

## Leading approach

1. Add a bounded local incident corpus and deterministic date/term gathering seam.
2. Make the gather checkpoint runnable with no model, then use it as the compact handoff to the
   existing separate model extraction path.
3. Capture the 85-minute specification, compatibility limit, recovery behaviour and Harness bonus.
4. Validate unit tests, build, and the no-model gather run. A live model rehearsal is a separate
   claim and must not be inferred from compilation.

## Refutation check

The tempting alternative is one tool-using typed GatherAgent. It is rejected for the portable core:
it directly recreates the measured local compatibility failure. Another alternative is bundling a
real public dataset now. It is rejected pending verified licensing/source metadata; synthetic data
is safer and is labelled as such. An MAF Workflows API implementation is deferred until the exact
package/API is validated; the fixed sequence is teachable first as explicit code and a workflow
diagram without guessing at a library surface.

## Completion criteria

- Dataset loading/filtering has tests for term, date, empty and cap behaviour.
- The command cannot read a caller-selected arbitrary path by default and returns a bounded pack.
- Specification describes responsibilities, timing, limits, recovery and non-goals.
- Existing solution builds and deterministic test suite passes.
- Any inability to invoke a local model is reported as unverified, not converted into a success
  claim.
