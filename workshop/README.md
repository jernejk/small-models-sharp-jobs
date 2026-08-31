# Attendee labs

Open exactly one numbered folder at a time. Each is a self-contained .NET build with its own
package configuration, data where required, README, and acceptance command.

Labs 01 and 02 are deliberately bare: one project, one `Program.cs`, two packages, and no data, no
`.env.example` and no tests. Configure them with `dotnet user-secrets` or shell variables. The
crash sample, the command switch and the test projects arrive at lab 03.

| Lab | You build | Model needed? |
|---|---|---|
| [01 — Getting started](01-getting-started/) | One local hello agent | Yes, after the initial build |
| [02 — Typed JSON](02-typed-json/) | A small parsed response contract | Yes, after the initial build |
| [03 — Gather](03-gather/) | A typed QueryAgent whose filter C# validates, then deterministic bounded retrieval | Yes for `query`; the `gather --term` debug check needs none |
| [04 — Extract](04-extract/) | Focused, tool-free selection | Yes |
| [05 — Analyse](05-analyse/) | Grounded analysis after a code gate | Yes |
| [06 — Workflow](06-workflow/) | The complete explicit pipeline | Yes |

Do not start in `facilitator/`; it contains presenter material, research, reference runs, and a
completed recovery solution.
