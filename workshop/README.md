# Attendee labs

Open exactly one numbered folder at a time. Each is a self-contained .NET build with its own
package configuration, data where required, README, and acceptance command.

Labs 01, 02 and 03 are deliberately bare: a single flat project each, no subcommands, and the prompt
as the only input. Defaults live in `appsettings.json`; override them with `dotnet user-secrets` or
shell variables, in that order of precedence. Lab 03 adds the crash sample and splits into
`Program.cs`, `GatherAgent.cs`, `Models.cs` and `Utilities.cs`. The command switch and the test
projects arrive at lab 04.

| Lab | You build | Model needed? |
|---|---|---|
| [01 — Getting started](01-getting-started/) | One local hello agent | Yes, after the initial build |
| [02 — Typed JSON](02-typed-json/) | A small parsed response contract | Yes, after the initial build |
| [03 — Gather](03-gather/) | A `GatherAgent` whose filter C# validates, then deterministic bounded retrieval | Yes |
| [04 — Extract](04-extract/) | Focused, tool-free selection | Yes |
| [05 — Analyse](05-analyse/) | Grounded analysis after a code gate | Yes |
| [06 — Workflow](06-workflow/) | The complete explicit pipeline | Yes |

Do not start in `facilitator/`; it contains presenter material, research, reference runs, and a
completed recovery solution.
