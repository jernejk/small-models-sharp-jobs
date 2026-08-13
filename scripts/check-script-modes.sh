#!/usr/bin/env bash
# Every shell script must be executable in the working tree *and* in the git index, or a fresh
# clone hands the attendee a file they have to chmod before the documented command works.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO"
PROBLEMS=0

for script in scripts/*.sh; do
  [[ -x "$script" ]] || { echo "not executable on disk: $script" >&2; PROBLEMS=$((PROBLEMS + 1)); }

  mode="$(git ls-files --stage -- "$script" | awk '{print $1}')"
  if [[ -z "$mode" ]]; then
    echo "not tracked by git: $script" >&2
    PROBLEMS=$((PROBLEMS + 1))
  elif [[ "$mode" != "100755" ]]; then
    echo "git records mode $mode (wanted 100755): $script" >&2
    PROBLEMS=$((PROBLEMS + 1))
  fi

  head -1 "$script" | grep -q '^#!' || { echo "no shebang: $script" >&2; PROBLEMS=$((PROBLEMS + 1)); }
  grep -q 'set -euo pipefail' "$script" || { echo "missing 'set -euo pipefail': $script" >&2; PROBLEMS=$((PROBLEMS + 1)); }
done

[[ $PROBLEMS -eq 0 ]] && echo "SCRIPT MODES: PASS ($(ls -1 scripts/*.sh | wc -l) scripts)"
exit "$PROBLEMS"
