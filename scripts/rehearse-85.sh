#!/usr/bin/env bash
# Times the machine half of the 85-minute path: every build, test and run an attendee waits on,
# starting from a clean starter tree. It does NOT measure typing, reading or explanation, so the
# total is a floor, not a rehearsal. A non-author human still has to sit the real thing.
#
# Every step asserts its expected outcome. A step that behaves unexpectedly fails the script,
# because a rehearsal that cannot fail measures nothing.
#
# The model-backed payoff needs a loaded local model; skip it with SKIP_MODEL=1.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORK="${WORK:-/tmp/rehearse-85}"
LOG="$(mktemp)"
trap 'rm -f "$LOG"' EXIT
TOTAL=0
PROBLEMS=0

# EXPECT=<code> marks a step whose non-zero exit is the point, such as an unfinished TODO gate.
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
  dotnet test "$WORK/Workshop.slnx" -c Release --no-restore >"$LOG" 2>&1 || true
  elapsed=$(echo "$(date +%s.%N) - $start" | bc)
  TOTAL=$(echo "$TOTAL + $elapsed" | bc)
  # -oP is GNU-only; a facilitator rehearsing on macOS silently got zeroes for every count.
  passed=$(grep -oE 'Passed:[[:space:]]+[0-9]+' "$LOG" | grep -oE '[0-9]+' | paste -sd+ - | bc || echo 0)
  failed=$(grep -oE 'Failed:[[:space:]]+[0-9]+' "$LOG" | grep -oE '[0-9]+' | paste -sd+ - | bc || echo 0)
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

echo "=== 85-minute mechanical rehearsal"
rm -rf "$WORK"
cp -r "$REPO/starter" "$WORK"
rm -rf "$WORK"/src/*/bin "$WORK"/src/*/obj "$WORK"/tests/*/bin "$WORK"/tests/*/obj

echo "--- minute 0: attendee opens the starter"
step "dotnet restore (warm NuGet cache)" dotnet restore "$WORK/Workshop.slnx"
step "dotnet build (first, cold)" dotnet build "$WORK/Workshop.slnx" -c Release --no-restore
# The checkpoints the agenda tells facilitators to call out. Quoted in the docs, so drift must fail here.
WANT_PASSED="${STARTER_PASSED:-31}" WANT_FAILED="${STARTER_FAILED:-0}" test_step "dotnet test (deterministic, green)"

echo "--- CP-03: deterministic Gather, no model involved"
step "dotnet run -- gather --term intersection" dotnet run --project "$WORK/src/Workshop.App" -c Release -- gather --term intersection
step "dotnet run -- gather --term cyclist (empty pack)" dotnet run --project "$WORK/src/Workshop.App" -c Release -- gather --term cyclist

echo "--- before TODO 4 and 5: the gate refuses to pass an unfinished Extract"
EXPECT=2 step "dotnet run -- run (caution branch)" dotnet run --project "$WORK/src/Workshop.App" -c Release -- run --term intersection

echo "--- TODO 4 and 5: the two typed model steps"
cp "$REPO/solution/src/Workshop.App/CrashPipeline.cs" "$WORK/src/Workshop.App/CrashPipeline.cs"
step "dotnet build after TODO 4+5" dotnet build "$WORK/Workshop.slnx" -c Release --no-restore
WANT_PASSED="${AFTER_TODOS_PASSED:-31}" WANT_FAILED=0 test_step "dotnet test after TODO 4+5"

echo "--- the payoff: a supported model-backed run"
if [[ "${SKIP_MODEL:-0}" == "1" ]]; then
  echo "  model-backed payoff SKIPPED (SKIP_MODEL=1)"
else
  step "dotnet run -- run --term intersection" dotnet run --project "$WORK/src/Workshop.App" -c Release -- run --term intersection
  step "dotnet run -- ready --term intersection" dotnet run --project "$WORK/src/Workshop.App" -c Release -- ready --term intersection
fi

echo
printf 'MECHANICAL TOTAL: %.1fs (%.1f minutes of waiting)\n' "$TOTAL" "$(echo "$TOTAL / 60" | bc -l)"
echo
echo "This measures machine time only. The 85-minute agenda is not rehearsed until a non-author"
echo "human sits it end to end."

if [[ $PROBLEMS -eq 0 ]]; then
  echo "REHEARSE_85: PASS"
else
  echo "REHEARSE_85: FAIL ($PROBLEMS unexpected outcome(s))" >&2
fi
exit "$PROBLEMS"
