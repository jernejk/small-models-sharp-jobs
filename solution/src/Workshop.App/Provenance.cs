using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Workshop.App;

/// <summary>
/// What produced a gate report, recorded next to the numbers so nobody has to trust a README for
/// the machine, the runtime or the weights. Anything this process cannot observe says so instead
/// of being filled in from memory.
/// </summary>
internal sealed record Provenance(
    string CapturedAtUtc,
    string Lane,
    string Endpoint,
    string Model,
    string ModelDigest,
    string ModelQuantization,
    string ModelParameterSize,
    string RuntimeVersion,
    string Placement,
    IReadOnlyDictionary<string, string> Settings,
    IReadOnlyDictionary<string, string> Hardware)
{
    private const string Unavailable = "unavailable";

    public static async Task<Provenance> CaptureAsync(ModelSettings settings, TimeSpan budget)
    {
        var runtime = Unavailable;
        var digest = Unavailable;
        var quantization = Unavailable;
        var parameterSize = Unavailable;
        var placement = Unavailable;

        if (settings.Lane == ModelLane.Local)
        {
            using var http = new HttpClient { BaseAddress = new Uri(settings.RuntimeApiRoot + "/"), Timeout = budget };
            runtime = await TextAsync(http, "api/version", root => root.GetProperty("version").GetString()) ?? Unavailable;

            var tags = await ElementAsync(http, "api/tags");
            var entry = FindModel(tags, "models", settings.Model);
            if (entry is { } model)
            {
                digest = Short(Value(model, "digest"));
                if (model.TryGetProperty("details", out var details))
                {
                    quantization = Value(details, "quantization_level");
                    parameterSize = Value(details, "parameter_size");
                }
            }

            placement = await PlacementAsync(http, settings.Model);
        }

        return new Provenance(
            DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            settings.LaneLabel,
            settings.Endpoint,
            settings.Model,
            digest,
            quantization,
            parameterSize,
            runtime,
            placement,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["temperature"] = "0",
                ["reasoningEffort"] = "none",
                ["reasoningOutput"] = "none",
                ["streaming"] = "off",
                ["contextLength"] = "runtime default (not set by this application)",
                ["requestBudgetSeconds"] = ((int)settings.RequestBudget.TotalSeconds).ToString(CultureInfo.InvariantCulture),
                ["prosesourcesSentToModel"] = string.Join(", ", IncidentPipeline.ProseSources)
            },
            DescribeHardware());
    }

    /// <summary>Ollama reports how many of the model's bytes are resident in VRAM; that is the placement number.</summary>
    private static async Task<string> PlacementAsync(HttpClient http, string model)
    {
        var running = await ElementAsync(http, "api/ps");
        if (FindModel(running, "models", model) is not { } entry) return "model not resident (run the pipeline first)";

        if (!entry.TryGetProperty("size", out var size) || !entry.TryGetProperty("size_vram", out var vram)) return Unavailable;

        var total = size.GetInt64();
        var onGpu = vram.GetInt64();
        if (total <= 0) return Unavailable;

        var gpuPercent = (int)Math.Round(100.0 * onGpu / total);
        return $"{100 - gpuPercent}% CPU / {gpuPercent}% GPU ({onGpu:N0} of {total:N0} bytes in VRAM)";
    }

    private static IReadOnlyDictionary<string, string> DescribeHardware()
    {
        var hardware = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["os"] = RuntimeInformation.OSDescription,
            ["architecture"] = RuntimeInformation.OSArchitecture.ToString(),
            ["dotnet"] = RuntimeInformation.FrameworkDescription,
            ["logicalCores"] = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture),
            ["cpu"] = FirstMatch("/proc/cpuinfo", "model name") ?? Unavailable,
            ["memory"] = FirstMatch("/proc/meminfo", "MemTotal") ?? Unavailable,
            ["gpu"] = "not enumerable from this process; see placement for what the runtime actually offloaded"
        };
        return hardware;
    }

    private static string? FirstMatch(string path, string key)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var line = File.ReadLines(path).FirstOrDefault(l => l.StartsWith(key, StringComparison.Ordinal));
            return line?.Split(':', 2) is { Length: 2 } parts ? parts[1].Trim() : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static JsonElement? FindModel(JsonElement? root, string arrayName, string model)
    {
        if (root is not { } element || !element.TryGetProperty(arrayName, out var models)) return null;

        foreach (var candidate in models.EnumerateArray())
        {
            var name = Value(candidate, "name");
            if (name == model || name == model + ":latest" || name.StartsWith(model + ":", StringComparison.Ordinal))
            {
                return candidate;
            }
        }
        return null;
    }

    private static string Value(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value.GetString() ?? Unavailable : Unavailable;

    private static string Short(string digest) =>
        digest.Length > 16 && digest != Unavailable ? digest[..16] : digest;

    private static async Task<JsonElement?> ElementAsync(HttpClient http, string path)
    {
        try
        {
            using var document = JsonDocument.Parse(await http.GetStringAsync(path));
            return document.RootElement.Clone();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return null;
        }
    }

    private static async Task<string?> TextAsync(HttpClient http, string path, Func<JsonElement, string?> select)
    {
        var element = await ElementAsync(http, path);
        return element is { } root ? select(root) : null;
    }
}
