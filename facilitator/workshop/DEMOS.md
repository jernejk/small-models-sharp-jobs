# Demo scripts

## Bounded evidence

```bash
cd workshop/06-workflow
dotnet run --project src/Workshop.App -- gather --term intersection --max 3
dotnet run --project src/Workshop.App -- gather --term cyclist
```

Show compact attributed records and the clean no-evidence branch. No model call occurs.

## Explicit calls and workflow

With a loaded local model, run `run --prompt "Show up to 5 intersection crashes from 2012."`, then `workflow --prompt "Show up to 5 intersection crashes from 2012."`. Extract selected IDs must exist in Gather; Analyse receives only validated selected records. Low confidence and unsupported output are successful caution branches.
