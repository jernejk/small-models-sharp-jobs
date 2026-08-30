# MAF Workflows spike — 30 Aug 2026

Proof that Microsoft.Agents.AI.Workflows 1.17 can express Gather → Extract → Validate → Analyse with conditional edges. Not in starter/ or solution/: `InProcessExecution.RunAsync` swallows executor exceptions (they arrive as `WorkflowErrorEvent`), so an endpoint-down run exits 0 and `LastOrDefault()` over an empty output reads as `Supported` (enum value 0). The lab keeps the plain-C# sequence.

Runs on M4 Max, LM Studio nvidia-nemotron-3-nano-4b:

```
--term intersection  gate: Supported  invoked: gather, extract, analyse, report  elapsed: 4224 ms
--term cyclist       gate: NoEvidence invoked: gather, report                   elapsed: 47 ms
MAF_ENDPOINT=http://localhost:9/v1 --term cyclist      gate: NoEvidence  exit 0  (no model call exists)
MAF_ENDPOINT=http://localhost:9/v1 --term intersection pipeline error: Connection refused  exit 3 (after WorkflowErrorEvent handling)
```

## Source

```csharp
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;
using Workshop.App;
using Workshop.Core;

var dataset = "/Users/jk/Developer/personal/pocs/global-ai-construct-offline-workshop/workshop/data/victoria-road-crash-sample.json";
var term = Arg("--term");
var question = Arg("--question") ?? "What patterns should an insurance and safety analyst review in the selected crash records?";
var started = Stopwatch.GetTimestamp();

var gather = Step<string, Stage>("gather", t =>
{
    var pack = IncidentDataset.Gather(IncidentDataset.Load(dataset), new IncidentQuery(null, null, t, 8));
    return new Stage(pack, null, [], null, pack.IsEmpty ? CrashGate.NoEvidence : null);
});

var extract = AsyncStep<Stage, Stage>("extract", async s =>
{
    var selection = await Typed<CrashSelection>(Agents.Extract, $"Question: {question}\nEvidence pack JSON:\n{WorkshopJson.Serialize(s.Pack)}");
    var gate = CrashWorkflow.ValidateSelection(s.Pack, selection, out var selected);
    return s with { Selection = selection, Selected = selected, Gate = gate is CrashGate.Supported ? null : gate };
});

var analyse = AsyncStep<Stage, Stage>("analyse", async s =>
{
    var analysis = await Typed<CrashAnalysis>(Agents.Analyse, $"Question: {question}\nValidated selected records JSON:\n{WorkshopJson.Serialize(s.Selected)}");
    return s with { Analysis = analysis, Gate = CrashWorkflow.ValidateAnalysis(s.Selected, analysis) };
});

var report = Step<Stage, CrashGate>("report", s =>
{
    Console.WriteLine($"gate: {s.Gate}");
    if (s.Selection is not null) Console.Write("extract JSON:\n" + WorkshopJson.Serialize(s.Selection));
    if (s.Analysis is not null) Console.Write("analyse JSON:\n" + WorkshopJson.Serialize(s.Analysis));
    return s.Gate!.Value;
});

var workflow = new WorkflowBuilder(gather)
    .AddEdge<Stage>(gather, report, s => s is { Gate: not null })
    .AddEdge<Stage>(gather, extract, s => s is { Gate: null })
    .AddEdge<Stage>(extract, report, s => s is { Gate: not null })
    .AddEdge<Stage>(extract, analyse, s => s is { Gate: null })
    .AddEdge(analyse, report)
    .WithOutputFrom(report)
    .Build();

Console.WriteLine($"workflow | term={term ?? "(none)"}");
await using var run = await InProcessExecution.RunAsync(workflow, term ?? "");

foreach (var e in run.OutgoingEvents.OfType<ExecutorInvokedEvent>()) Console.WriteLine($"invoked: {e.ExecutorId}");
Console.WriteLine($"elapsed: {Stopwatch.GetElapsedTime(started).TotalMilliseconds:F0} ms");

// RunAsync swallows executor exceptions; without this rethrow a dead endpoint exits 0 with no output.
if (run.OutgoingEvents.OfType<WorkflowErrorEvent>().Select(e => e.Data).OfType<Exception>().FirstOrDefault() is { } failure)
{
    Console.Error.WriteLine($"pipeline error: {failure.GetBaseException().Message}");
    return 3;
}

var final = run.OutgoingEvents.OfType<WorkflowOutputEvent>().Select(e => e.Data).OfType<CrashGate>().Cast<CrashGate?>().LastOrDefault();
return final is CrashGate.Supported or CrashGate.NoEvidence ? 0 : 2;

static ExecutorBinding Step<TIn, TOut>(string id, Func<TIn, TOut> run) => run.BindAsExecutor(id);
static ExecutorBinding AsyncStep<TIn, TOut>(string id, Func<TIn, ValueTask<TOut>> run) => run.BindAsExecutor(id);

static async ValueTask<T?> Typed<T>(AIAgent agent, string prompt) where T : class
{
    var response = await agent.RunAsync<T>(prompt);
    try { return response.Result; }
    catch (Exception ex) when (ex is InvalidOperationException or JsonException) { return null; }
}

string? Arg(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

internal sealed record Stage(EvidencePack Pack, CrashSelection? Selection, IReadOnlyList<IncidentRecord> Selected, CrashAnalysis? Analysis, CrashGate? Gate);

internal static class Agents
{
    private static readonly IChatClient Chat = CreateChatClient();

    public static AIAgent Extract { get; } = Agent("ExtractAgent", """
        Select only relevant records from the provided evidence pack for the user's question.
        Return recordIds from the pack exactly, a short rationale, and 0-100 confidence.
        Do not invent records, facts, causes, or recommendations. You have no tools.
        """);

    public static AIAgent Analyse { get; } = Agent("AnalyseAgent", """
        Analyse only the compact structured records provided. Return a grounded finding, practical actions,
        open questions, and 0-100 confidence. Do not claim causation not present in the records.
        """);

    private static IChatClient CreateChatClient()
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri((Environment.GetEnvironmentVariable("MAF_ENDPOINT") ?? "http://localhost:1234/v1").TrimEnd('/') + "/"),
            NetworkTimeout = TimeSpan.FromSeconds(90),
            RetryPolicy = new ClientRetryPolicy(maxRetries: 0)
        };
        options.AddPolicy(new MaxTokensCompatibilityPolicy(), PipelinePosition.PerCall);
        return new OpenAIClient(new ApiKeyCredential("lm-studio"), options).GetChatClient(Environment.GetEnvironmentVariable("MAF_MODEL") ?? "nvidia-nemotron-3-nano-4b").AsIChatClient();
    }

    private static AIAgent Agent(string name, string instructions) => Chat.AsAIAgent(new ChatClientAgentOptions
    {
        Name = name,
        ChatOptions = new ChatOptions { Instructions = instructions, Temperature = 0, MaxOutputTokens = 700, Reasoning = new ReasoningOptions { Effort = ReasoningEffort.None, Output = ReasoningOutput.None } }
    });
}
```
