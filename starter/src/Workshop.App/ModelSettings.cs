using System.Globalization;

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

    public static ModelSettings FromEnvironment() => From(
        Environment.GetEnvironmentVariable("MAF_ENDPOINT"),
        Environment.GetEnvironmentVariable("MAF_API_KEY"),
        Environment.GetEnvironmentVariable("MAF_MODEL"),
        Environment.GetEnvironmentVariable("MAF_TIMEOUT_SECONDS"));

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
