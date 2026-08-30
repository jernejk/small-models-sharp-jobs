# LUNA slide QA — 2026-08-29 — pass 2

## Result

**PASS.** The current local file and published URL are at parity and pass the requested visual, responsive, deep-link, print, keyboard, network, and content-contract checks. No remaining defects were found.

The deck was not edited. Tested with Python Playwright 1.62.0 and Chromium using:

- Local: `file:///Users/jk/Developer/personal/pocs/global-ai-construct-offline-workshop/workshop/slides/index.html`
- Published: `https://small-models-sharp-jobs-workshop-20260829.surge.sh/slides/`
- Viewports: `1920x1080`, `1366x768`, `390x844`
- Fresh browser contexts for every viewport and deep-link target

Evidence is in [`pass2/run.json`](pass2/run.json), with 162 per-slide screenshots under [`pass2/screenshots/`](pass2/screenshots/) and four PDFs under [`pass2/pdf/`](pass2/pdf/).

## Per-slide visual result

Every row was opened and screenshot at all three viewports on both lanes. All rows passed: no visible clipping, overlap, horizontal document scroll, broken asset, console error, or local/published text difference.

| # | Slide | Local | Published | Result |
|---:|---|---|---|---|
| 1 | Small Models, Sharp Jobs. | PASS | PASS | PASS |
| 2 | Jernej Kavka (JK) | PASS | PASS | PASS |
| 3 | A workflow you can see. | PASS | PASS | PASS |
| 4 | Build along, or observe. Both work. | PASS | PASS | PASS |
| 5 | Smaller context. Sharper work. | PASS | PASS | PASS |
| 6 | Prove the lane, then give it a contract. | PASS | PASS | PASS |
| 7 | One prompt. One answer. No tools. | PASS | PASS | PASS |
| 8 | Useful output needs a shape. | PASS | PASS | PASS |
| 9 | Evidence first. Reason later. | PASS | PASS | PASS |
| 10 | A bounded query, not arbitrary files. | PASS | PASS | PASS |
| 11 | The model may ask. It may not reach. | PASS | PASS | PASS |
| 12 | Ask the model to select, not to conclude. | PASS | PASS | PASS |
| 13 | Selected IDs, rationale, confidence. | PASS | PASS | PASS |
| 14 | High effort on a small set. | PASS | PASS | PASS |
| 15 | Only validated records reach this call. | PASS | PASS | PASS |
| 16 | The model proposes. Code decides. | PASS | PASS | PASS |
| 17 | Every branch is a success. | PASS | PASS | PASS |
| 18 | Same sequence. Visible topology. | PASS | PASS | PASS |
| 19 | Catch up without catching fire. | PASS | PASS | PASS |
| 20 | Three ways back into the room. | PASS | PASS | PASS |
| 21 | Close and hidden extras. | PASS | PASS | PASS |
| 22 | Teach the boundary, not the benchmark. | PASS | PASS | PASS |
| 23 | Small jobs. Clear evidence. Provable outcomes. | PASS | PASS | PASS |

Hidden presenter extras were also opened and screenshot on both lanes and all viewports:

| Hidden id | Result |
|---|---|
| `extra-harness` | PASS |
| `extra-mcp` | PASS |
| `extra-benchmark` | PASS |
| `extra-access` | PASS |

## Check results

### 1. Local/published parity and network

| Viewport | Local sections | Published sections | Normalized text | Hidden ids |
|---|---:|---:|---|---|
| 1920x1080 | 27 total / 23 visible | 27 total / 23 visible | Identical | Identical four ids |
| 1366x768 | 27 total / 23 visible | 27 total / 23 visible | Identical | Identical four ids |
| 390x844 | 27 total / 23 visible | 27 total / 23 visible | Identical | Identical four ids |

The hidden ids are exactly `extra-harness`, `extra-mcp`, `extra-benchmark`, and `extra-access`. All 27 normalized section texts matched in DOM order at every viewport pair.

Published asset requests were all HTTP 200 at every viewport:

- `/assets/jk-mvp-banner.png`
- `/assets/jk-logo.png`
- `/assets/buildclub.png`
- `/assets/local-agent-flow-cover-v1.png`

Cross-origin requests: **0** on every local and published lane/viewport. Request failures: **0**. Console errors: **0**. The local lane loaded the corresponding four `file://` assets.

### 2. Cold deep links and true reload

Fresh contexts were used for `#5`, `#12`, `#extra-harness`, and `#extra-access`. A real `page.reload()` was then performed on `#5`.

| Target | Local cold result | Published cold result | Reload result |
|---|---|---|---|
| `#5` | `The core sequence`, `5 / 23`, hash `#5` | Same | PASS: remained `The core sequence`, `5 / 23`, hash `#5` |
| `#12` | `Up next: Extract`, `12 / 23`, hash `#12` | Same | N/A |
| `#extra-harness` | `Extra: Workflow vs Harness`, `presenter extra` | Same | N/A |
| `#extra-access` | `Extra: Hosted access`, `presenter extra` | Same | N/A |

Each deep link had exactly one active slide.

### 3. Computed typography at 1366x768

Measured every matching element on all 27 sections on both lanes: **48 elements per lane** — 6 `pre`, 18 `.card span`, 9 `.hint`, and 15 `.sub`.

- Minimum computed size: **24.0416px**
- Maximum computed size: **25.271px**
- Elements below 24px: **0 local, 0 published**

No below-threshold elements require listing.

### 4. Overflow and clipping

Every normal slide and hidden extra was measured after activation at all three viewports on both lanes.

| Viewport | Max document scroll size | Elements outside active slide | Slides with true overflow |
|---|---|---:|---:|
| 1920x1080 | `1920x1080` | 0 | 0 |
| 1366x768 | `1366x768` | 0 | 0 |
| 390x844 | `390x844` | 0 | 0 |

Long commands wrapped within their code boxes on the phone viewport. No active-slide descendant exceeded the slide bounds.

### 5. Print/PDF

Generated from both desktop (`1920x1080`) and phone (`390x844`) contexts on both lanes, using print media, print backgrounds, and the deck’s CSS page size.

| PDF | Pages | Hidden slides excluded | Page size |
|---|---:|---|---|
| `local-desktop.pdf` | 23 | Yes; 4 excluded | 960x540pt |
| `local-phone.pdf` | 23 | Yes; 4 excluded | 960x540pt |
| `published-desktop.pdf` | 23 | Yes; 4 excluded | 960x540pt |
| `published-phone.pdf` | 23 | Yes; 4 excluded | 960x540pt |

Page 23 passed the QR check in all four PDFs. In the desktop print layout the QR panel measured **224x224px**, with text beginning at `x=419.83px` after the QR right edge at `x=377.59px`. In the phone-context print layout it measured **154x154px**, with text beginning at `x=193.77px` after the QR right edge at `x=185.19px`. The rendered page 23 screenshot confirms a compact white QR panel with the share text beside it and no large blank expansion.

### 6. `h` toggle

Both lanes passed the two-toggle sequence from `#5`:

`5 / 23` → first `h` → `5 / 27`, presenter badge visible → second `h` → `5 / 23`, presenter badge hidden.

The hash remained `#5` throughout. Toggle count: **2 presses tested per lane**.

### 7. Content contract

`src/Workshop.App/Program.cs` usage contains all six required app commands:

`smoke`, `typed`, `gather`, `run`, `workflow`, `ready`.

The unique application commands shown in slide code blocks are `smoke`, `typed`, `gather`, `run`, and `workflow`; all are present in usage. `ready` is usage-listed and available for the supported-path rehearsal, though it is not shown as a slide code-block invocation. The setup slide’s `dotnet test` is the repository test command, not a `Workshop.App` subcommand.

Checkpoint acceptance text matches `workshop/CHECKPOINTS.md`: CP-01 and CP-02 are verbatim; CP-03 through CP-06 preserve the same acceptance conditions with concise explanatory wording on the slides. Specifically, the deck covers the bounded date/term/cap and no-result Gather branches, unknown/duplicate/malformed Extract rejection, low-confidence caution, and the rule that no evidence bypasses Extract and Analyse.

## Remaining defects

None. P0 published deployment parity, P1 cold deep links, P2 support typography and print QR layout, and P3 scroll extent all passed this final re-test.

Inspected: every visible slide and hidden extra, both lanes, all three viewports, normalized DOM text, hidden-id filtering, four published asset responses, cross-origin/request/console state, all requested fresh deep links, true `#5` reload, all 48 typography targets per lane, active-slide/document overflow metrics, four 23-page PDFs, rendered PDF page 23, two `h` toggles per lane, and the app/checkpoint content contract.
