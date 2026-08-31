using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Workshop.App;

internal enum ModelLane { Local, Hosted }

/// <summary>
/// Everything the provider seam needs, and nothing secret. The lane is derived from the endpoint
/// rather than declared, so a run cannot print LOCAL while talking to somebody else's server.
/// </summary>
internal sealed record ModelSettings(string Endpoint, string ApiKey, string Model, TimeSpan RequestBudget)
{
    public const string DefaultEndpoint = "http://localhost:11434/v1";
    public const string DefaultModel = "nemotron-3-nano:4b";
    public const int DefaultBudgetSeconds = 90;

    // Precedence: shell variables > dotnet user-secrets > .env > defaults.
    public static ModelSettings FromEnvironment()
    {
        var stored = new ConfigurationBuilder()
            .AddInMemoryCollection(ReadDotEnv())
            .AddUserSecrets(typeof(ModelSettings).Assembly, optional: true)
            .Build();
        return From(Get("MAF_ENDPOINT"), Get("MAF_API_KEY"), Get("MAF_MODEL"), Get("MAF_TIMEOUT_SECONDS"));
        string? Get(string key) => Environment.GetEnvironmentVariable(key) ?? stored[key];
    }

    private static Dictionary<string, string?> ReadDotEnv()
    {
        var path = Path.Combine(WorkshopPaths.RepoRoot(), ".env");
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (!File.Exists(path)) return values;
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            if (line.StartsWith("export ", StringComparison.Ordinal)) line = line[7..].TrimStart();
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            values[line[..eq].Trim()] = line[(eq + 1)..].Trim().Trim('"', '\'');
        }
        return values;
    }

    public static ModelSettings From(string? endpoint, string? apiKey, string? model, string? budgetSeconds) => new(
        Or(endpoint, DefaultEndpoint),
        Or(apiKey, "ollama"),
        Or(model, DefaultModel),
        ParseBudget(budgetSeconds));

    public ModelLane Lane => IsLoopback(Endpoint) ? ModelLane.Local : ModelLane.Hosted;

    public string LaneLabel => Lane == ModelLane.Local ? "LOCAL" : "HOSTED";

    /// <summary>Ollama's native API sits beside the OpenAI-compatible one, not under it.</summary>
    public string RuntimeApiRoot => Endpoint.TrimEnd('/') is var trimmed && trimmed.EndsWith("/v1", StringComparison.Ordinal)
        ? trimmed[..^3].TrimEnd('/')
        : trimmed;

    public static bool IsLoopback(string? endpoint) =>
        Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
        && (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase));

    /// <summary>Clamped so a typo cannot disable the ceiling the 90 s gate depends on.</summary>
    public static TimeSpan ParseBudget(string? seconds) =>
        TimeSpan.FromSeconds(int.TryParse(seconds, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Clamp(parsed, 5, 600)
            : DefaultBudgetSeconds);

    private static string Or(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
