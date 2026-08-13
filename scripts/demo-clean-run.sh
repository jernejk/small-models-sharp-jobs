#!/usr/bin/env bash
# Demo 1: the clean run. Shows the three artifacts and the claims code refused to vouch for.
# Exits non-zero if anything the facilitator is about to say out loud is not true.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO"

fail() { echo "DEMO-CLEAN-RUN: FAIL - $*" >&2; exit 1; }

echo "=== the evidence pack the model is allowed to read"
ls -1 evidence-pack | sed 's/^/  /'
echo "  (expected-facts.json is present but NOT on the tool whitelist)"
echo

# Stale artifacts would let this demo "pass" on last week's output.
rm -f artifacts/claim-ledger.json artifacts/verification.json artifacts/incident-brief.md

status=0
dotnet run --project src/Workshop.App -c Release -- run || status=$?
echo
[[ $status -eq 0 ]] || fail "clean run exited $status, wanted 0 (2=verification failed, 5=tool contract broken)"

for artifact in claim-ledger.json verification.json incident-brief.md; do
  [[ -s "artifacts/$artifact" ]] || fail "artifacts/$artifact was not written"
done

python3 - <<'PY' || fail "the clean run did not have the shape this demo describes"
import json, sys
report = json.load(open("artifacts/verification.json"))
ledger = json.load(open("artifacts/claim-ledger.json"))
brief = open("artifacts/incident-brief.md").read()
problems = []
if report["failed"]:
    problems.append(f"{report['failed']} verification failure(s)")
if report["unverified"] < 1:
    problems.append("nothing was marked UNVERIFIED, so the teaching moment is missing")
if ledger["incidentId"] != "INC-042":
    problems.append(f"ledger incident id is {ledger['incidentId']!r}")
if "stale routing rule identified" not in brief:
    problems.append("the code-parsed timeline is missing from the brief")
if "billing system" in brief.split("## Timeline")[0]:
    problems.append("the customer's cause reached Verified facts")
print("; ".join(problems) or "shape ok")
sys.exit(1 if problems else 0)
PY

echo "=== what code refused to call a fact"
sed -n '/## Shown but not verified/,/## Excluded/p' artifacts/incident-brief.md | head -14
echo
echo "=== what the evidence log actually says happened"
grep -i "routing rule" evidence-pack/events.csv | sed 's/^/  /'
echo
echo "DEMO-CLEAN-RUN: PASS (exit 0, three artifacts, at least one UNVERIFIED, no cause promoted)"
