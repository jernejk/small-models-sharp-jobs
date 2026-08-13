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
check_fails "starter/ is red before the TODOs" dotnet test starter/Workshop.slnx -c Release --no-build

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
if [[ "${SKIP_MODEL:-0}" == "1" ]]; then
  echo "model-backed demo SKIPPED (SKIP_MODEL=1)"
else
  check "demo 1 clean run" bash scripts/demo-clean-run.sh
fi
check "demo 2 break it, every seeded defect" bash scripts/demo-break-it.sh

echo "=== local model (needs the runtime up)"
if [[ "${SKIP_MODEL:-0}" == "1" ]]; then
  echo "local model gates SKIPPED (SKIP_MODEL=1)"
else
  check "smoke" dotnet run --project src/Workshop.App -c Release --no-build -- smoke
  check "attendee readiness" dotnet run --project src/Workshop.App -c Release --no-build -- ready
  check "gates L1-L6, 5 repetitions" dotnet run --project src/Workshop.App -c Release --no-build -- gates --repeat 5
fi

echo
if [[ $FAILURES -eq 0 ]]; then
  echo "VERIFY_ALL: PASS"
else
  echo "VERIFY_ALL: FAIL ($FAILURES check(s))"
fi
exit "$FAILURES"
