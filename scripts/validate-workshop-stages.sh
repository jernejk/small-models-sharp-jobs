#!/usr/bin/env bash
set -euo pipefail

# Reproducible, secret-safe validation for the attendee path. It only prints endpoint/model labels
# supplied by the environment; do not add or echo API-key values here.
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
stages=(01-getting-started 02-typed-json 03-gather 04-extract 05-analyse 06-workflow)

# Labs 01-02 are a single Program.cs with no test project; tests start at lab 03.
for stage in "${stages[@]}"; do
  echo "== $stage: build =="
  dotnet build "$root/workshop/$stage/src/Workshop.App/Workshop.App.csproj" --nologo
  if [[ -f "$root/workshop/$stage/Workshop.slnx" ]]; then
    echo "== $stage: test =="
    dotnet test "$root/workshop/$stage/Workshop.slnx" --nologo
  fi
done

echo "== 03-gather: deterministic empty-pack check =="
dotnet run --project "$root/workshop/03-gather/src/Workshop.App" -- gather --term definitely-not-present

if [[ -n "${MAF_ENDPOINT:-}" && -n "${MAF_MODEL:-}" ]]; then
  echo "== 06-workflow: configured model path =="
  echo "endpoint=${MAF_ENDPOINT} model=${MAF_MODEL}"
  dotnet run --project "$root/workshop/06-workflow/src/Workshop.App" -- smoke
  dotnet run --project "$root/workshop/06-workflow/src/Workshop.App" -- typed
  dotnet run --project "$root/workshop/06-workflow/src/Workshop.App" -- query --prompt "Show up to 5 intersection crashes from 2012."
  dotnet run --project "$root/workshop/06-workflow/src/Workshop.App" -- run --prompt "Show up to 5 intersection crashes from 2012."
  dotnet run --project "$root/workshop/06-workflow/src/Workshop.App" -- workflow --prompt "Show up to 5 intersection crashes from 2012."
else
  echo "SKIP model path: set MAF_ENDPOINT and MAF_MODEL to validate the configured local model."
fi
