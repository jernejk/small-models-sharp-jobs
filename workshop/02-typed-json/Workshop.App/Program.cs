using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

var settings = ModelSettings.FromEnvironment();
Console.WriteLine($"local hello | {settings.Lane} | endpoint={settings.Endpoint} | model={settings.Model}");

try
{
    var client = new OpenAIClient(
            new ApiKeyCredential(settings.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(settings.Endpoint) })
        .GetChatClient(settings.Model)
        .AsIChatClient();

    AIAgent helloAgent = client.AsAIAgent(
        instructions: "Reply with exactly the token you are given.",
        name: "HelloAgent");

    var response = await helloAgent.RunAsync("Reply with exactly this token: WORKSHOP_OK");
    Console.WriteLine(response.Text);

    // TODO: CP-02 make a second agent, ask it for a SimpleCommand, and print the action and target.
}
catch (Exception ex)
{
    var root = ex; while (root.InnerException is not null) root = root.InnerException;
    Console.Error.WriteLine($"call failed: {root.Message}");
    Console.Error.WriteLine("Check `lms ps` or `ollama ps`, then the endpoint and model name above.");
    return 1;
}

return 0;

/// <summary>The smallest typed contract that still has something to check: two named strings.</summary>
internal sealed record SimpleCommand(string Action, string Target);

/// <summary>Shell variables beat user-secrets, user-secrets beat these defaults.</summary>
internal sealed record ModelSettings(string Endpoint, string ApiKey, string Model)
{
    public static ModelSettings FromEnvironment()
    {
        var secrets = new ConfigurationBuilder().AddUserSecrets(typeof(ModelSettings).Assembly, optional: true).Build();
        return new(
            Read("MAF_ENDPOINT") ?? "http://localhost:11434/v1",
            Read("MAF_API_KEY") ?? "ollama",
            Read("MAF_MODEL") ?? "nemotron-3-nano:4b");

        string? Read(string key) => Environment.GetEnvironmentVariable(key) ?? secrets[key];
    }

    public string Lane => Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) && uri.IsLoopback ? "LOCAL" : "HOSTED";
}
