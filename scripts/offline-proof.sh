#!/usr/bin/env bash
# Tier A offline proof. Run as root on the reference machine:
#   sudo scripts/offline-proof.sh
#
# Blocks every non-loopback packet for the workshop user, then proves the full path still
# restores from the local NuGet cache and produces all three artifacts. Ollama listens on
# loopback under its own uid, so the model stays reachable while the internet does not.
#
# Tier B (physically pulling Wi-Fi) is a human rehearsal step and cannot be automated here.
set -euo pipefail

WORKSHOP_USER="${WORKSHOP_USER:-fleet-jackdaw}"
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UID_NUMBER="$(id -u "$WORKSHOP_USER")"
NUGET_CONFIG=/tmp/offline-proof-nuget.config
OUT_DIR="$REPO/artifacts"
FAILURES=0

[[ $EUID -eq 0 ]] || { echo "must run as root (needs iptables)"; exit 1; }

cleanup() {
  iptables -D OUTPUT -m owner --uid-owner "$UID_NUMBER" '!' -o lo -j REJECT 2>/dev/null || true
  echo "--- egress restored for $WORKSHOP_USER"
}
trap cleanup EXIT

as_user() { su - "$WORKSHOP_USER" -c "cd '$REPO' && $1"; }

check() {
  local label="$1"; shift
  if "$@" >/dev/null 2>&1; then
    echo "  PASS  $label"
  else
    echo "  FAIL  $label"
    FAILURES=$((FAILURES + 1))
  fi
}

cat > "$NUGET_CONFIG" <<'XML'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
  </packageSources>
</configuration>
XML
chmod 644 "$NUGET_CONFIG"

echo "=== Tier A offline proof: $REPO"
echo "--- blocking non-loopback egress for $WORKSHOP_USER (uid $UID_NUMBER)"
iptables -A OUTPUT -m owner --uid-owner "$UID_NUMBER" '!' -o lo -j REJECT || { echo "could not install iptables rule"; exit 1; }

echo "--- proving the block is real"
if as_user "curl -sS --max-time 8 https://api.nuget.org/v3/index.json" >/dev/null 2>&1; then
  echo "  FAIL  external HTTPS still reachable - the block did not take effect"
  exit 1
fi
echo "  PASS  external HTTPS is unreachable"
check "loopback still reachable (Ollama)" as_user "curl -sS --max-time 8 http://localhost:11434/v1/models"

echo "--- restoring from the local NuGet cache with no remote sources"
as_user "rm -rf src/*/obj tests/*/obj"
check "dotnet restore (cache only)" as_user "dotnet restore Workshop.slnx --configfile '$NUGET_CONFIG'"
check "dotnet build" as_user "dotnet build Workshop.slnx -c Release --no-restore"

echo "--- deterministic tests with no network"
check "dotnet test" as_user "dotnet test Workshop.slnx -c Release --no-build"

echo "--- full local path with no network"
as_user "rm -f artifacts/claim-ledger.json artifacts/verification.json artifacts/incident-brief.md"
check "pipeline run" as_user "dotnet run --project src/Workshop.App -c Release --no-build -- run"
for artifact in claim-ledger.json verification.json incident-brief.md; do
  check "produced $artifact" test -s "$OUT_DIR/$artifact"
done

echo "--- attendee readiness with no network"
check "ready" as_user "dotnet run --project src/Workshop.App -c Release --no-build -- ready"

echo "--- seeded defects with no network"
check "break it (every seeded defect)" as_user "bash scripts/demo-break-it.sh"

echo
if [[ $FAILURES -eq 0 ]]; then
  echo "OFFLINE_PROOF: PASS (no non-loopback egress, local cache only, all three artifacts)"
else
  echo "OFFLINE_PROOF: FAIL ($FAILURES check(s) failed)"
fi
exit "$FAILURES"
