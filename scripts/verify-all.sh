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
check "canonical tree builds" dotnet build Workshop.slnx -c Release
check "deterministic tests" dotnet test Workshop.slnx -c Release --no-build
check "starter/ and solution/ match src/" python3 scripts/generate-starter.py --check
check "starter/ compiles" dotnet build starter/Workshop.slnx -c Release
check "solution/ compiles" dotnet build solution/Workshop.slnx -c Release
check "solution/ tests are green" dotnet test solution/Workshop.slnx -c Release --no-build

echo "=== attendee experience"
# The TODOs live in the App, not in Core, so the deterministic suite stays green in starter/.
# What the unfinished tree cannot do is clear a gate: `run` takes the caution branch and exits 2.
check_fails "starter/ cannot clear a gate before the TODOs" \
  dotnet run --project starter/src/Workshop.App -c Release --no-build -- run --term intersection

echo "=== distribution"
if [[ "${SKIP_PACKAGING:-0}" == "1" ]]; then
  echo "packaging checks SKIPPED (SKIP_PACKAGING=1)"
else
  check "repository has a commit" git -C "$REPO" rev-parse HEAD
  check "no git remote is configured" test -z "$(git -C "$REPO" remote)"
  check "tree is clean" test -z "$(git -C "$REPO" status --porcelain)"
  check "scripts are committed executable" bash scripts/check-script-modes.sh
  check "a clean clone builds and tests" bash scripts/check-distribution.sh clone
  check "a git archive builds and tests" bash scripts/check-distribution.sh archive
fi

echo "=== demos"
check "bounded Gather returns records" dotnet run --project src/Workshop.App -c Release --no-build -- gather --term intersection
check "no-result Gather is a clean empty pack" dotnet run --project src/Workshop.App -c Release --no-build -- gather --term cyclist

echo "=== local model (needs the runtime up)"
if [[ "${SKIP_MODEL:-0}" == "1" ]]; then
  echo "local model gates SKIPPED (SKIP_MODEL=1)"
else
  check "smoke" dotnet run --project src/Workshop.App -c Release --no-build -- smoke
  check "typed contract" dotnet run --project src/Workshop.App -c Release --no-build -- typed
  check "attendee readiness" dotnet run --project src/Workshop.App -c Release --no-build -- ready --term intersection
  check "model-backed tests" env WORKSHOP_LOCAL_MODEL=1 dotnet test tests/Workshop.LocalModel.Tests -c Release --no-build
fi

echo
if [[ $FAILURES -eq 0 ]]; then
  echo "VERIFY_ALL: PASS"
else
  echo "VERIFY_ALL: FAIL ($FAILURES check(s))"
fi
exit "$FAILURES"
