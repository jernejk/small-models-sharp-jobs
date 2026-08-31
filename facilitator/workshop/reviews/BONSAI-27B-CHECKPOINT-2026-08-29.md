# Bonsai 27B checkpoint probe

This is a small empirical probe, not a model ranking or a claim that the model can complete the
workshop unattended.

## Question

Can the locally served Bonsai 27B model complete one named, narrow coding checkpoint from the
repository instructions?

## Method

- Runtime: local LM Studio, `prism-ml/bonsai-27b` (the installed 2-bit local build).
- Task: CP-01 / the original starter TODO 1, `EvidenceStore.TryRead`.
- Contract: return only a JSON object containing the C# method body; preserve the whitelist-first
  boundary, initialise outputs, provide useful errors, reject path escape and missing files, and do
  not change other files.
- Three temperature-zero calls used `reasoning_effort: none` and a 650-token completion cap.
- The first response was applied only to an isolated copy of `starter/`; it was never applied to
  the workshop repository.

## Result

| Check | Result |
| --- | --- |
| JSON response parseable | 3/3 |
| C# response compiled in the isolated starter copy | yes |
| Existing `EvidenceStoreTests` passed after applying response one | 14/14 |
| Full requested contract met | **no**: 3/3 responses returned an empty error for blank input, despite the explicit requirement for `"empty evidence id"` |

The model consistently produced a plausible, narrowly scoped implementation and cleared the current
focused tests. It nevertheless missed an explicit acceptance requirement that those tests do not
currently assert. That is precisely the kind of half-finish a reviewer must catch.

## Decision

Use Bonsai 27B as an optional assisted-coding or code-review comparison in the workshop. Do not use
this probe to claim it can autonomously complete arbitrary checkpoints. Give it one named checkpoint
at a time, require tests, and have a separate review pass check the written contract rather than only
the green test result.

The model was unloaded after the probe. CP-03 Gather, CP-04 Extract, CP-05 Analyse and CP-06 workflow
were not tested by this probe.
