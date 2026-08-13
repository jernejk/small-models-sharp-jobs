#!/usr/bin/env bash
# Put the tree back the way an attendee found it, between runs or between sessions.
# Replaces `git checkout artifacts/`, which never worked: artifacts/ is ignored, so git has
# nothing to restore and the command fails with "did not match any file(s) known to git".
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO"

git rev-parse --git-dir >/dev/null 2>&1 || {
  echo "not a git repository - nothing to restore from" >&2
  exit 1
}

echo "=== restoring tracked files that a demo may have edited"
git checkout -- evidence-pack/ src/ tests/
git status --porcelain -- evidence-pack/ src/ tests/ | sed 's/^/  still modified: /'

echo "=== clearing generated artifacts (ignored by git, so it must be a delete)"
rm -rf artifacts/break-it
rm -f artifacts/claim-ledger.json artifacts/verification.json artifacts/incident-brief.md artifacts/gate-report.json
find . -path ./workshop -prune -o -name 'artifacts' -type d -print | while read -r dir; do
  find "$dir" -mindepth 1 ! -name '.gitkeep' -delete
done

echo "=== measured evidence that is kept on purpose"
ls -1 workshop/reference-run | sed 's/^/  /'

echo
echo "RESET: done. Tracked files restored, artifacts cleared, workshop/reference-run/ untouched."
