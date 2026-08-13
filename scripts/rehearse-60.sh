#!/usr/bin/env bash
# Times the machine half of the 60-minute path: every build, test and run an attendee waits on,
# starting from a clean starter tree. It does NOT measure typing, reading or explanation, so the
# total is a floor, not a rehearsal. A non-author human still has to sit the real thing.
#
# Every step asserts its expected outcome. A step that behaves unexpectedly fails the script,
# because a rehearsal that cannot fail measures nothing.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORK="${WORK:-/tmp/rehearse-60}"
LOG="$(mktemp)"
trap 'rm -f "$LOG"' EXIT
TOTAL=0
PROBLEMS=0

# EXPECT=<code> marks a step whose non-zero exit is the point, such as a caught seeded defect.
step() {
  local label="$1"; shift
  local expected="${EXPECT:-0}" actual=0 start elapsed outcome
  start=$(date +%s.%N)
  "$@" >"$LOG" 2>&1 || actual=$?
  elapsed=$(echo "$(date +%s.%N) - $start" | bc)
  TOTAL=$(echo "$TOTAL + $elapsed" | bc)
  if [[ "$actual" -eq "$expected" ]]; then
    outcome="ok (exit $actual)"
  else
    outcome="UNEXPECTED (exit $actual, wanted $expected)"
    PROBLEMS=$((PROBLEMS + 1))
  fi
  printf '  %-46s %6.1fs  %s\n' "$label" "$elapsed" "$outcome"
  [[ "$actual" -eq "$expected" ]] || tail -8 "$LOG" | sed 's/^/      /'
}

# WANT_PASSED / WANT_FAILED assert the checkpoint an attendee is told to look for.
test_step() {
  local label="$1"
  local want_passed="${WANT_PASSED:-}" want_failed="${WANT_FAILED:-}"
  local start elapsed passed failed
  start=$(date +%s.%N)
  dotnet test "$WORK/Workshop.slnx" -c Release >"$LOG" 2>&1 || true
  elapsed=$(echo "$(date +%s.%N) - $start" | bc)
  TOTAL=$(echo "$TOTAL + $elapsed" | bc)
  passed=$(grep -oP 'Passed:\s+\K\d+' "$LOG" | paste -sd+ | bc || echo 0)
  failed=$(grep -oP 'Failed:\s+\K\d+' "$LOG" | paste -sd+ | bc || echo 0)
  printf '  %-46s %6.1fs  %s passed / %s failed' "$label" "$elapsed" "${passed:-?}" "${failed:-?}"

  if [[ -n "$want_passed" && "${passed:-0}" != "$want_passed" ]]; then
    printf '  UNEXPECTED (wanted %s passed)' "$want_passed"
    PROBLEMS=$((PROBLEMS + 1))
  fi
  if [[ -n "$want_failed" && "${failed:-0}" != "$want_failed" ]]; then
    printf '  UNEXPECTED (wanted %s failed)' "$want_failed"
    PROBLEMS=$((PROBLEMS + 1))
  fi
  printf '\n'
}

echo "=== 60-minute mechanical rehearsal"
rm -rf "$WORK"
cp -r "$REPO/starter" "$WORK"
rm -rf "$WORK"/src/*/bin "$WORK"/src/*/obj "$WORK"/tests/*/bin "$WORK"/tests/*/obj

echo "--- minute 0: attendee opens the starter"
step "dotnet restore (warm NuGet cache)" dotnet restore "$WORK/Workshop.slnx"
step "dotnet build (first, cold)" dotnet build "$WORK/Workshop.slnx" -c Release --no-restore
WANT_PASSED="${STARTER_PASSED:-}" WANT_FAILED="${STARTER_FAILED:-}" test_step "dotnet test (expected red)"

echo "--- TODO 1: constrain the evidence tool"
cp "$REPO/solution/src/Workshop.Core/EvidenceStore.cs" "$WORK/src/Workshop.Core/EvidenceStore.cs"
WANT_PASSED="${AFTER_TODO1_PASSED:-}" test_step "dotnet test after TODO 1"

echo "--- TODO 2 and 3: register the tool, connect typed extraction"
cp "$REPO/solution/src/Workshop.App/IncidentPipeline.cs" "$WORK/src/Workshop.App/IncidentPipeline.cs"
step "dotnet build after TODO 2+3" dotnet build "$WORK/Workshop.slnx" -c Release

echo "--- TODO 4: the first verification rule"
cp "$REPO/solution/src/Workshop.Core/Verifier.cs" "$WORK/src/Workshop.Core/Verifier.cs"
WANT_FAILED=0 test_step "dotnet test after TODO 4 (expected green)"

echo "--- the payoff: three artifacts"
step "dotnet run -- run" dotnet run --project "$WORK/src/Workshop.App" -c Release --no-build -- run --evidence "$WORK/evidence-pack" --out "$WORK/artifacts"
for artifact in claim-ledger.json verification.json incident-brief.md; do
  if [[ ! -s "$WORK/artifacts/$artifact" ]]; then
    echo "  MISSING $artifact"
    PROBLEMS=$((PROBLEMS + 1))
  fi
done

echo "--- break it: one seeded defect (exit 2 is the lesson)"
EXPECT=2 step "dotnet run -- verify-only --inject-defect" dotnet run --project "$WORK/src/Workshop.App" -c Release --no-build -- verify-only --inject-defect altered-number --ledger "$WORK/artifacts/claim-ledger.json" --evidence "$WORK/evidence-pack" --out "$WORK/artifacts/break-it/altered-number"

echo
printf 'MECHANICAL TOTAL: %.1fs (%.1f minutes of waiting)\n' "$TOTAL" "$(echo "$TOTAL / 60" | bc -l)"
echo "artifacts: $(ls -1 "$WORK/artifacts" 2>/dev/null | tr '\n' ' ')"
echo
echo "This measures machine time only. The 60-minute agenda is not rehearsed until a non-author"
echo "human sits it end to end."

if [[ $PROBLEMS -eq 0 ]]; then
  echo "REHEARSE_60: PASS"
else
  echo "REHEARSE_60: FAIL ($PROBLEMS unexpected outcome(s))" >&2
fi
exit "$PROBLEMS"
