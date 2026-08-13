#!/usr/bin/env bash
# Demo 2: break it. Every seeded defect must be rejected on exactly its intended rule.
# Each defect writes a complete, self-consistent trio into its own directory. The clean
# artifacts are read and never written, so nothing here can leave the tree half-corrupted.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO"

CLEAN_LEDGER="artifacts/claim-ledger.json"
RUN_ID="${RUN_ID:-$(date -u +%Y%m%dT%H%M%SZ)}"
RUN_DIR="artifacts/break-it/$RUN_ID"
FAILURES=0

[[ -s "$CLEAN_LEDGER" ]] || {
  echo "no ledger at $CLEAN_LEDGER - run scripts/demo-clean-run.sh first" >&2
  exit 1
}
CLEAN_BEFORE="$(sha256sum "$CLEAN_LEDGER" | cut -d' ' -f1)"

# defect:expected rule:expected status:expected exit code
CASES=(
  "phantom-source:R1-SOURCE-WHITELIST:FAIL:2"
  "altered-number:R5-AFFECTED-CUSTOMERS:FAIL:2"
  "altered-timestamp:R6-TIMESTAMP:FAIL:2"
  "mislabelled-cause:R12-KIND-SEMANTICS:FAIL:2"
  "spliced-quote:R2-QUOTE-PRESENT:FAIL:2"
  "unsupported-event:R11-EVENT-SUPPORTED:UNVERIFIED:0"
)

echo "=== break it | run $RUN_ID"
echo

for entry in "${CASES[@]}"; do
  IFS=: read -r defect rule expected_status expected_exit <<<"$entry"
  out="$RUN_DIR/$defect"
  mkdir -p "$out"

  echo "=== $defect  (predict before you look: $rule = $expected_status)"

  status=0
  dotnet run --project src/Workshop.App -c Release -- verify-only \
    --inject-defect "$defect" --ledger "$CLEAN_LEDGER" --out "$out" >"$out/console.log" 2>&1 || status=$?

  problems="$(python3 - "$out" "$rule" "$expected_status" <<'PY'
import json, sys
out, rule, expected_status = sys.argv[1], sys.argv[2], sys.argv[3]
problems = []
try:
    ledger = json.load(open(f"{out}/claim-ledger.json"))
    report = json.load(open(f"{out}/verification.json"))
    brief = open(f"{out}/incident-brief.md").read()
except Exception as exc:
    print(f"artifacts unreadable: {exc}")
    sys.exit(0)

if not any(r["ruleId"] == rule and r["status"] == expected_status for r in report["results"]):
    problems.append(f"{rule}={expected_status} never fired")

failed = sorted({r["ruleId"] for r in report["results"] if r["status"] == "FAIL"})
wanted = [rule] if expected_status == "FAIL" else []
if failed != wanted:
    problems.append(f"failed rules {failed or ['none']}, wanted {wanted or ['none']}")

# The three files must describe the same corrupted run, not two different ones.
ledger_ids = {c["id"] for c in ledger["claims"]}
graded = {r["claimId"] for r in report["results"]} - {"(ledger)"}
if not graded <= ledger_ids:
    problems.append(f"report grades claims absent from this ledger: {sorted(graded - ledger_ids)}")
for counter in (f"- passed: {report['passed']}", f"- failed: {report['failed']}", f"- unverified: {report['unverified']}"):
    if counter not in brief:
        problems.append(f"brief missing {counter!r}")

verified = brief.split("## Verified facts")[1].split("\n## ")[0]
for claim_id in ("C900", "C901", "C902", "C903", "C904", "C905"):
    if f"| {claim_id} |" in verified:
        problems.append(f"{claim_id} reached Verified facts")
print("; ".join(problems))
PY
)"

  echo "  exit code   : $status (wanted $expected_exit)"
  echo "  artifacts   : $out"
  grep -E "^(failed rules|rejected|touched claims)" "$out/console.log" | sed 's/^/  /'

  # The app's own verdict: intended rule fired AND nothing it touched reached verified facts.
  grep -qE '^rejected +: yes' "$out/console.log" || problems="${problems:+$problems; }not rejected"

  if [[ "$status" -ne "$expected_exit" ]]; then
    echo "  MISMATCH - exit $status, wanted $expected_exit"
    FAILURES=$((FAILURES + 1))
  elif [[ -n "$problems" ]]; then
    echo "  MISMATCH - $problems"
    FAILURES=$((FAILURES + 1))
  else
    echo "  OK - rejected on the intended rule, artifacts self-consistent, clean trio untouched"
  fi
  echo
done

CLEAN_AFTER="$(sha256sum "$CLEAN_LEDGER" | cut -d' ' -f1)"
if [[ "$CLEAN_BEFORE" != "$CLEAN_AFTER" ]]; then
  echo "MISMATCH - the clean ledger was modified by this demo" >&2
  FAILURES=$((FAILURES + 1))
fi

echo "clean ledger unchanged: ${CLEAN_BEFORE:0:12} -> ${CLEAN_AFTER:0:12}"
if [[ $FAILURES -eq 0 ]]; then
  echo "DEMO-BREAK-IT: PASS (${#CASES[@]} defects, each rejected on exactly its intended rule)"
else
  echo "DEMO-BREAK-IT: FAIL ($FAILURES of ${#CASES[@]} defects behaved unexpectedly)" >&2
fi
exit "$FAILURES"
