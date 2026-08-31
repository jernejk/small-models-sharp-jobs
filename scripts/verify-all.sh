#!/usr/bin/env bash
# Everything that must be true before this workshop is presented.
# Local model gates need a running runtime; skip them with SKIP_MODEL=1.
# Packaging checks clone the repo; skip them with SKIP_PACKAGING=1.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO"
FAILURES=0
LOG="$(mktemp)"
trap 'rm -f "$LOG"' EXIT

check() {
  local label="$1"; shift
  printf '%-54s' "$label"
  if "$@" >"$LOG" 2>&1; then
    echo "PASS"
  else
    echo "FAIL"
    FAILURES=$((FAILURES + 1))
    sed 's/^/    /' "$LOG" | tail -10
  fi
}

# For steps whose failure is the point.
check_fails() {
  local label="$1"; shift
  printf '%-54s' "$label"
  if "$@" >"$LOG" 2>&1; then
    echo "FAIL (it succeeded; the check cannot fail, so it is not a check)"
    FAILURES=$((FAILURES + 1))
  else
    echo "PASS"
  fi
}

echo "=== deterministic"
check "facilitator solution builds" dotnet build facilitator/reference/solution/Workshop.slnx -c Release
check "facilitator deterministic tests" dotnet test facilitator/reference/solution/Workshop.slnx -c Release --no-build
check "attendee stages build and deterministic checks pass" scripts/validate-workshop-stages.sh

echo "=== attendee experience"
check "attendee map is present" test -f workshop/README.md

echo "=== distribution"
if [[ "${SKIP_PACKAGING:-0}" == "1" ]]; then
  echo "packaging checks SKIPPED (SKIP_PACKAGING=1)"
else
  check "repository has a commit" git -C "$REPO" rev-parse HEAD
  check "the clone target remote is configured" git -C "$REPO" remote get-url origin
  check "tree is clean" test -z "$(git -C "$REPO" status --porcelain)"
  check "scripts are committed executable" bash scripts/check-script-modes.sh
  check "a clean clone builds and tests" bash scripts/check-distribution.sh clone
  check "a git archive builds and tests" bash scripts/check-distribution.sh archive
fi

echo "=== demos"
check "completed Gather returns records" dotnet run --project facilitator/reference/solution/src/Workshop.App -c Release --no-build -- gather --term intersection
check "completed no-result Gather is clean" dotnet run --project facilitator/reference/solution/src/Workshop.App -c Release --no-build -- gather --term cyclist

echo "=== local model (needs the runtime up)"
if [[ "${SKIP_MODEL:-0}" == "1" ]]; then
  echo "local model gates SKIPPED (SKIP_MODEL=1)"
else
  check "smoke" dotnet run --project facilitator/reference/solution/src/Workshop.App -c Release --no-build -- smoke
  check "typed contract" dotnet run --project facilitator/reference/solution/src/Workshop.App -c Release --no-build -- typed
  check "attendee readiness" dotnet run --project facilitator/reference/solution/src/Workshop.App -c Release --no-build -- ready --prompt "Show up to 5 intersection crashes from 2012."
  check "model-backed tests" env WORKSHOP_LOCAL_MODEL=1 dotnet test facilitator/reference/solution/tests/Workshop.LocalModel.Tests -c Release --no-build
fi

echo
if [[ $FAILURES -eq 0 ]]; then
  echo "VERIFY_ALL: PASS"
else
  echo "VERIFY_ALL: FAIL ($FAILURES check(s))"
fi
exit "$FAILURES"
