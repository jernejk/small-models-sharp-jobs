#!/usr/bin/env bash
# Run once on a real connection, before the workshop. After this, nothing needs the internet.
# Set WORKSHOP_PREFETCH_QWEN=1 to also pull the optional model-comparison weights (~3.4 GB).
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO"
MODEL="${MAF_MODEL:-nemotron-3-nano:4b}"
COMPARISON_MODEL="${WORKSHOP_COMPARISON_MODEL:-qwen3.5:4b}"
MISSING=0

fail() { echo; echo "PREFETCH: FAIL - $*" >&2; exit 1; }

need() {
  local label="$1" cmd="$2" hint="$3"
  printf '%-28s' "$label"
  if command -v "$cmd" >/dev/null 2>&1; then
    echo "found: $("$cmd" --version 2>&1 | head -1)"
  else
    echo "MISSING - $hint"
    MISSING=$((MISSING + 1))
  fi
}

echo "=== prerequisites"
need "dotnet SDK" dotnet "https://dotnet.microsoft.com/download/dotnet/10.0"
need "ollama" ollama "https://ollama.com/download"
need "python3" python3 "used by the drift check and the demo scripts"
[[ $MISSING -eq 0 ]] || fail "install the missing prerequisites above, then run this again"

echo
echo "=== model weights (~2.8 GB)"
if ollama list 2>/dev/null | grep -q "^${MODEL%%:*}"; then
  echo "already pulled: $MODEL"
else
  ollama pull "$MODEL" || fail "could not pull $MODEL - check the runtime is running"
fi

if [[ "${WORKSHOP_PREFETCH_QWEN:-0}" == "1" ]]; then
  echo
  echo "=== optional comparison weights (~3.4 GB, only for the 120-minute model-swap extension)"
  if ollama list 2>/dev/null | grep -q "^${COMPARISON_MODEL%%:*}"; then
    echo "already pulled: $COMPARISON_MODEL"
  else
    ollama pull "$COMPARISON_MODEL" || fail "could not pull $COMPARISON_MODEL"
  fi
else
  echo
  echo "(skipping the optional $COMPARISON_MODEL comparison model; set WORKSHOP_PREFETCH_QWEN=1 to include it)"
fi

echo
echo "=== NuGet packages (needs network exactly once)"
dotnet restore workshop/06-workflow/Workshop.slnx || fail "dotnet restore failed"

echo
cd "$REPO/workshop/06-workflow"
echo "=== proving the final lab works offline from here"
dotnet build Workshop.slnx -c Release --no-restore >/dev/null || fail "build failed"

test_log="$(mktemp)"
trap 'rm -f "$test_log"' EXIT
dotnet test Workshop.slnx -c Release --no-build >"$test_log" 2>&1 || {
  tail -20 "$test_log" >&2
  fail "deterministic tests did not pass"
}
grep -E "^Passed!" "$test_log" | sed 's/^/  /'

status=0
dotnet run --project src/Workshop.App -c Release --no-build -- ready --prompt "Show up to 5 intersection crashes from 2012." || status=$?
[[ $status -eq 0 ]] || fail "readiness check exited $status - this machine cannot produce the three artifacts yet"

echo
echo "PREFETCH COMPLETE - you can disconnect. Bring the laptop as-is."
