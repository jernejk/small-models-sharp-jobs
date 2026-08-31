using System.ClientModel;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

var prompt = args.Length > 0 ? string.Join(' ', args) : "Show up to 5 intersection crashes from 2012.";

if (string.IsNullOrWhiteSpace(prompt))
{
    Console.WriteLine("""Give me a prompt, for example: dotnet run -- "Show up to 5 intersection crashes from 2012." """);
    return;
}

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

string Required(string key) => config[key] ?? throw new InvalidOperationException($"{key} is not set in appsettings.json.");

var settings = new ModelSettings(Required("MAF_ENDPOINT"), Required("MAF_API_KEY"), Required("MAF_MODEL"));
Console.WriteLine($"workflow | {settings.Lane} | model={settings.Model}");
Console.WriteLine($"prompt: {prompt}");

var client = new OpenAIClient(
        new ApiKeyCredential(settings.ApiKey),
        new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) })
    .GetChatClient(settings.Model)
    .AsIChatClient();

var records = Utilities.Load();
Console.WriteLine($"corpus: {records.Count} record(s)");

var gather = new GatherExecutor(client, records);
var extract = new ExtractExecutor(client, prompt);
var analyse = new AnalyseExecutor(client, prompt);
var report = new ReportExecutor();

// The order is fixed here, in code. A gate set upstream routes straight to report,
// so there is no path from a prompt to an analysis that skips Extract or its gate.
var workflow = new WorkflowBuilder(gather)
    .AddEdge<CrashRun>(gather, report, run => run is { Gate: not null })
    .AddEdge<CrashRun>(gather, extract, run => run is { Gate: null })
    .AddEdge<CrashRun>(extract, report, run => run is { Gate: not null })
    .AddEdge<CrashRun>(extract, analyse, run => run is { Gate: null })
    .AddEdge(analyse, report)
    .WithOutputFrom(report)
    .Build();

await using var run = await InProcessExecution.RunAsync(workflow, prompt);

foreach (var invoked in run.OutgoingEvents.OfType<ExecutorInvokedEvent>())
    Console.WriteLine($"invoked: {invoked.ExecutorId}");

// InProcessExecution.RunAsync does NOT throw when an executor fails: the exception arrives as a
// WorkflowErrorEvent. Skip this check and a dead endpoint produces no output, no error, and exit 0.
if (run.OutgoingEvents.OfType<WorkflowErrorEvent>().Select(e => e.Data).OfType<Exception>().FirstOrDefault() is { } failure)
{
    var root = failure; while (root.InnerException is not null) root = root.InnerException;
    Console.WriteLine($"workflow error: {root.Message}");
    Console.WriteLine("Check `lms ps` or `ollama ps`, then the endpoint and model name above.");
}
