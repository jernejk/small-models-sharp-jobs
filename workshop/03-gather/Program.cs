using System.ClientModel;
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
Console.WriteLine($"gather | {settings.Lane} | model={settings.Model}");
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
    var agent = GatherAgent.Create(client);

    // TODO: CP-03 ask the agent for a QueryFilter, validate it, then gather.
}
catch (Exception ex)
{
    var root = ex; while (root.InnerException is not null) root = root.InnerException;
    Console.WriteLine($"call failed: {root.Message}");
    Console.WriteLine("Check `lms ps` or `ollama ps`, then the endpoint and model name above.");
}
