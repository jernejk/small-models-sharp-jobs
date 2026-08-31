# LUNA slide QA — 2026-08-29

## Result

FAIL for release parity and local deep-link startup. The current local deck is visually sound when settled, but the published URL is an older deck and does not match it. The current local deck also overwrites an initial `#5` or `#extra-*` hash before applying it.

Tested with Python Playwright 1.62.0 and Chromium at 1920x1080, 1366x768, and 390x844. The local lane used the permitted `file://` URL for `workshop/slides/index.html`; the published lane used:

`https://small-models-sharp-jobs-workshop-20260829.surge.sh/slides/`

Settled desktop screenshots are in this directory: `01.png`–`23.png` for the current local deck, `published/01.png`–`12.png` for the published deck, plus `extra-*.png`. Settled responsive captures are under `responsive-settled-local/` and `published/responsive-settled/`. The local hash-extra screenshots intentionally show the failure: each is the cover rather than the requested extra.

## Current local visible slides

| # | Title | Result | Exact issue |
|---:|---|---|---|
| 1 | Small Models, Sharp Jobs. | PASS | Cover image renders and is visibly different from the solid-background slides. No clipping, overlap, or contrast defect. |
| 2 | Jernej Kavka (JK) | PASS | Speaker illustration, links, and chips fit at desktop, laptop, and phone sizes. |
| 3 | A workflow you can see. | PASS | Agenda is legible and states 85 minutes; no visible clipping or overlap. |
| 4 | Build along, or observe. Both work. | WARN | Code and card supporting copy are below 24px at room/laptop scale; layout remains contained. |
| 5 | Smaller context. Sharper work. | PASS | Gather → Extract → gate → Analyse → gate is clear; no visible clipping or overlap. |
| 6 | Prove the lane, then give it a contract. | PASS | Interlude text and time range fit. |
| 7 | One prompt. One answer. No tools. | WARN | Environment block and acceptance hint are below 24px at room/laptop scale; no clipping. |
| 8 | Useful output needs a shape. | WARN | Card descriptions and acceptance hint are below 24px at room/laptop scale; no clipping. |
| 9 | Evidence first. Reason later. | PASS | Interlude text and time range fit. |
| 10 | A bounded query, not arbitrary files. | WARN | Two command lines and acceptance hint are below 24px at room/laptop scale; phone wrapping is contained. |
| 11 | The model may ask. It may not reach. | WARN | Card descriptions and recovery path are below 24px at room/laptop scale; no clipping. |
| 12 | Ask the model to select, not to conclude. | PASS | Interlude text and time range fit. |
| 13 | Selected IDs, rationale, confidence. | WARN | Command/JSON example and acceptance hint are below 24px at room/laptop scale; no clipping. |
| 14 | High effort on a small set. | PASS | Interlude text and time range fit. |
| 15 | Only validated records reach this call. | WARN | Card descriptions and acceptance hint are below 24px at room/laptop scale; no clipping. |
| 16 | The model proposes. Code decides. | PASS | Interlude text and time range fit. |
| 17 | Every branch is a success. | WARN | Card descriptions and acceptance hint are below 24px at room/laptop scale; no clipping. |
| 18 | Same sequence. Visible topology. | WARN | Two command lines and acceptance hint are below 24px at room/laptop scale; no clipping. |
| 19 | Catch up without catching fire. | PASS | Interlude text and time range fit. |
| 20 | Three ways back into the room. | WARN | Card descriptions, paths, and note are below 24px at room/laptop scale; no clipping. |
| 21 | Close and hidden extras. | PASS | Interlude text and time range fit. |
| 22 | Teach the boundary, not the benchmark. | PASS | Supporting paragraph fits and remains higher contrast than the background. |
| 23 | Small jobs. Clear evidence. Provable outcomes. | WARN | Screen layout fits, but print/PDF expands the QR white panel to a large blank block; see PDF findings. |

No obvious typos were found. The content matches the workshop’s deterministic Gather → typed Extract → code gate → typed Analyse flow over the Victorian crash sample. Commands shown locally were cross-checked against `README.md`, `workshop/CHECKPOINTS.md`, and `src/Workshop.App/Program.cs`; `smoke`, `typed`, `gather`, `run`, `workflow`, and `ready` are implemented/documented. No slide content adds arbitrary filesystem access or requires hosted access.

## Hidden presenter extras

| Hash | Current local result | Published result |
|---|---|---|
| `#extra-harness` | FAIL: loads cover, counter `1 / 23`, hash becomes `#1` | PASS: loads `Workflow versus Harness.`, counter `presenter extra` |
| `#extra-mcp` | FAIL: loads cover, counter `1 / 23`, hash becomes `#1` | PASS: loads `MCP is the tool plug.`, counter `presenter extra` |
| `#extra-benchmark` | FAIL: loads cover, counter `1 / 23`, hash becomes `#1` | PASS: loads `Benchmark only what you rehearse.`, counter `presenter extra` |
| `#extra-access` | FAIL: loads cover, counter `1 / 23`, hash becomes `#1` | PASS: loads `Hosted access is optional.`, counter `presenter extra` |

The local failure is caused by startup order: `refreshVisible()` calls `show()` and writes `#1` before the later hash branch can call `showById()`.

## Keyboard, counter, progress, and hash

### Current local deck

- ArrowRight, ArrowLeft, Space, PageDown, and PageUp all moved the deck. Each test changed the counter, progress width, and URL hash as expected.
- ArrowRight stopped at `23 / 23`; hidden slides were excluded from the normal counter.
- Progress examples: `4.34783%` at `1 / 23`, `8.69565%` at `2 / 23`, `13.0435%` at `3 / 23`.
- `#5` was observed before reload as `5 / 23`, but reload landed on `1 / 23` with hash `#1`: FAIL.
- `#extra-harness` reload landed on the cover with hash `#1`: FAIL.

### Published deck

- All five requested keys moved the deck and updated counter/progress/hash.
- ArrowRight stopped at `12 / 12`.
- Reloading `#5` landed on `5 / 12` with hash `#5`: PASS.
- `#extra-harness` loaded the extra and showed `presenter extra`: PASS.

## Viewports

| Viewport | Current local | Published URL |
|---|---|---|
| 1920x1080 | 23 visible slides. Settled screenshots showed no text outside the viewport, no internal text overflow, no element overlap, and no contrast failure. | 12 visible slides. Settled screenshots showed no visible clipping/overlap/contrast failure, but it is the older deck. |
| 1366x768 | 23 visible slides. No visible clipping or overlap. Supporting text measures about 13.9–21.2px, so code, hints, and card descriptions are too small for the back of a room. | 12 visible slides. No visible clipping/overlap; same small secondary-text limitation. |
| 390x844 | 23 visible slides. Long commands wrap inside their boxes; no visible clipping or internal overflow. Supporting text measures about 13.1–16.8px and is phone-readable but not room-readable. | 12 visible slides. The old Gather code block overflows horizontally (`scrollWidth` 437px vs 318px client width on slide 7); document width reached 474px, so the old published phone deck can clip/scroll horizontally. |

The current local DOM reports a 12px excess document height (`1092` at 1080px, `780` at 768px, `856` at 844px) because inactive slides retain their 12px transition transform. `html, body { overflow: hidden; }` prevents visible scrolling, and no active-slide content was clipped.

## Print/PDF

Generated with Playwright `page.pdf()` under print media with backgrounds enabled:

[luna-slide-qa-print.pdf](luna-slide-qa-print.pdf)

- Current local visible slides: 23.
- Hidden slides: 4; rendered print slides: 23.
- PDF page count: 23. Hidden slides were excluded.
- Page size: 1280x720 CSS pixels (`960x540` points).
- Cover background renders in the PDF; the rendered first page was visually checked.
- Page 23 has a layout defect: the white QR container expands across most of the page, leaving a large blank white area. The on-screen slide is compact and correct.

## Network requests

Requests were captured with Playwright request listeners and deduplicated by URL. There were no failed requests or console errors.

Local `file://` lane:

```text
file:///Users/jk/Developer/personal/pocs/global-ai-construct-offline-workshop/workshop/slides/index.html
file:///Users/jk/Developer/personal/pocs/global-ai-construct-offline-workshop/workshop/assets/jk-mvp-banner.png
file:///Users/jk/Developer/personal/pocs/global-ai-construct-offline-workshop/workshop/assets/jk-logo.png
file:///Users/jk/Developer/personal/pocs/global-ai-construct-offline-workshop/workshop/assets/buildclub.png
file:///Users/jk/Developer/personal/pocs/global-ai-construct-offline-workshop/workshop/assets/local-agent-flow-cover-v1.png
```

Published lane:

```text
https://small-models-sharp-jobs-workshop-20260829.surge.sh/slides/
https://small-models-sharp-jobs-workshop-20260829.surge.sh/assets/local-agent-flow-cover-v1.png
```

No request was off-machine and cross-origin relative to its lane. The published request list also confirms that it is not loading the current local speaker assets; it is serving the older 12-slide document.

## Local versus published comparison

FAIL. The local current deck has 23 visible slides plus 4 hidden extras. The published deck has 12 visible slides plus 4 hidden extras. Including generated/local DOM slides, text extraction returned 27 local sections versus 16 published sections, and the extracted text arrays were not equal. Examples of local-only content include the speaker slide, generated agenda, explicit section interludes, CP-04/CP-05/CP-06 detail, recovery lanes, QR share block, and the expanded presenter extras.

## Ranked defects and concrete fixes

1. **P0 — Published URL is stale.** Publish the current `workshop/slides/index.html` and its referenced assets to the published `/slides/` path, invalidate any stale CDN/Surge cache if needed, then rerun the parity check until both lanes report 23 visible slides, 4 hidden extras, and identical normalized text.
2. **P1 — Local startup hashes are overwritten.** Parse and preserve `location.hash` before the first `refreshVisible()`/`show()` call, or let the initial refresh call `show(..., false)`. Then apply `#5` and `#extra-*` after generated slides are built; verify fresh loads, not same-document hash changes only.
3. **P2 — Audience support text is too small for a room.** Raise code, card descriptions, hints, recovery paths, and metadata toward a room-safe minimum (approximately 24px at 1366x768), or move nonessential detail to presenter notes. Keep long commands wrapped and recheck the 390px phone layout.
4. **P2 — Print QR layout expands.** Constrain the QR grid item in print, for example with `justify-self:start` and an explicit/max-content width for `.share-qr`, then rerender page 23 and confirm the share text sits beside the compact QR card.
5. **P3 — Inactive-slide transform inflates DOM scroll extent.** Remove or contain the off-screen transition transform for inactive slides if exact document dimensions matter. This did not produce visible clipping because overflow is hidden.

Inspected and found clean: settled local screenshots for all 23 visible slides, the cover-vs-solid-background comparison, all requested key presses, counters/progress/hash behavior, local and published requests, printed page count/backgrounds, visible/hidden slide filtering, and the workshop command/content contract.
