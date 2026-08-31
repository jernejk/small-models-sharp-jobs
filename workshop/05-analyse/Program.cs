using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

var prompt = args.Length > 0 ? string.Join(' ', args) : "Show up to 5 intersection crashes from 2012.";

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

string Required(string key) => config[key] ?? throw new InvalidOperationException($"{key} is not set in appsettings.json.");

var settings = new ModelSettings(Required("MAF_ENDPOINT"), Required("MAF_API_KEY"), Required("MAF_MODEL"));
Console.WriteLine($"analyse | {settings.Lane} | model={settings.Model}");
Console.WriteLine($"prompt: {prompt}");

var client = new OpenAIClient(
        new ApiKeyCredential(settings.ApiKey),
        new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) })
    .GetChatClient(settings.Model)
    .AsIChatClient();

var records = Utilities.Load();
Console.WriteLine($"corpus: {records.Count} record(s)");

try
{
    var query = await GatherAgent.Create(client).RunAsync<QueryFilter>(prompt);
    var evidence = Utilities.Gather(records, Utilities.ValidateFilter(query.Result));
    Console.WriteLine($"gathered: {evidence.Count} record(s)");

    if (evidence.Count == 0)
    {
        Console.WriteLine("no evidence found: Extract and Analyse are never called.");
    }
    else
    {
        var extract = await ExtractAgent.Create(client).RunAsync<CrashSelection>(ExtractAgent.Prompt(prompt, evidence));
        var selection = Gates.TryTyped(extract);
        var gate = Gates.ValidateSelection(evidence, selection, out var selected);
        Console.WriteLine($"extract gate: {gate}");
        Console.WriteLine("selected: " + Utilities.ToJson(selected));

        if (gate is CrashGate.Supported)
        {
            CrashAnalysis? analysis = null;

            // TODO: CP-05 ask the AnalyseAgent for a CrashAnalysis over `selected`, and nothing else.

            Console.WriteLine($"analyse gate: {Gates.ValidateAnalysis(selected, analysis)}");
            Console.WriteLine("analysis: " + Utilities.ToJson(analysis));
        }
    }
}
catch (Exception ex)
{
    var root = ex; while (root.InnerException is not null) root = root.InnerException;
    Console.WriteLine($"call failed: {root.Message}");
    Console.WriteLine("Check `lms ps` or `ollama ps`, then the endpoint and model name above.");
}
