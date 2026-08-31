using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

var settings = ModelSettings.Load();
Console.WriteLine($"local hello | {settings.Lane} | endpoint={settings.Endpoint} | model={settings.Model}");

try
{
    // TODO: CP-01 build the chat client, turn it into an agent, and print its reply.
}
catch (Exception ex)
{
    var root = ex; while (root.InnerException is not null) root = root.InnerException;
    Console.Error.WriteLine($"call failed: {root.Message}");
    Console.Error.WriteLine("Check `lms ps` or `ollama ps`, then the endpoint and model name above.");
    return 1;
}

return 0;

/// <summary>Later sources win: shell variables beat user-secrets, which beat appsettings.json.</summary>
internal sealed record ModelSettings(string Endpoint, string ApiKey, string Model)
{
    public static ModelSettings Load()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        return new(Required("MAF_ENDPOINT"), Required("MAF_API_KEY"), Required("MAF_MODEL"));

        string Required(string key) => config[key] ?? throw new InvalidOperationException($"{key} is not set in appsettings.json.");
    }

    public string Lane => Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) && uri.IsLoopback ? "LOCAL" : "HOSTED";
}
