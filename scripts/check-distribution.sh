#!/usr/bin/env bash
# Proves the repository is distributable: a fresh clone (or a git archive) of the committed tree
# builds and passes the deterministic tests with no reference back to the working directory.
#
#   scripts/check-distribution.sh clone     git clone into a temp dir
#   scripts/check-distribution.sh archive   git archive HEAD | tar -x into a temp dir
set -euo pipefail

MODE="${1:-clone}"
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

case "$MODE" in
  clone)
    git clone --quiet "$REPO" "$WORK/repo"
    ;;
  archive)
    mkdir -p "$WORK/repo"
    git -C "$REPO" archive HEAD | tar -x -C "$WORK/repo"
    ;;
  *)
    echo "usage: check-distribution.sh [clone|archive]" >&2
    exit 2
    ;;
esac

cd "$WORK/repo"
echo "=== $MODE at $WORK/repo"

# The final documented attendee commands, run from the final numbered lab.
cd workshop/06-workflow
dotnet build Workshop.slnx -c Release
dotnet test Workshop.slnx -c Release --no-build
dotnet run --project src/Workshop.App -c Release --no-build -- gather --term intersection >"$WORK/gather.json"

grep -q '"id"' "$WORK/gather.json" || { echo "distribution FAIL: Gather returned no bounded records" >&2; exit 1; }

for script in "$WORK/repo"/scripts/*.sh; do
  [[ -x "$script" ]] || { echo "distribution FAIL: $script is not executable in $MODE" >&2; exit 1; }
done

echo "DISTRIBUTION ($MODE): PASS"
