#!/usr/bin/env bash
# Mechanical rehearsal of the numbered attendee stages. Set MAF_ENDPOINT and MAF_MODEL to include
# the configured final-lab model lane; no credential values are printed.
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
exec "$repo/scripts/validate-workshop-stages.sh"
