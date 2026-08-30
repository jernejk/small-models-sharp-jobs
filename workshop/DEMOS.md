# Demo scripts

## Bounded evidence

```bash
dotnet run --project src/Workshop.App -- gather --term intersection --max 3
dotnet run --project src/Workshop.App -- gather --term cyclist
```

Show compact attributed records and the clean no-evidence branch. No model call occurs.

## Explicit calls and workflow

With a loaded local model, run `run --term intersection`, then `workflow --term intersection`. Extract selected IDs must exist in Gather; Analyse receives only validated selected records. Low confidence and unsupported output are successful caution branches.
