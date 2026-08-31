using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Loading and filtering. No agent touches this: the model only ever produces a filter.</summary>
internal static class Utilities
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string ToJson<T>(T value) => JsonSerializer.Serialize(value, Json);

    public static IReadOnlyList<CrashRecord> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", "victoria-road-crash-sample.json");
        return JsonSerializer.Deserialize<List<CrashRecord>>(File.ReadAllText(path), Json)
            ?? throw new InvalidDataException("The crash sample was empty or invalid JSON.");
    }

    /// <summary>The gate. Whatever the model asked for, these bounds are what the corpus actually sees.</summary>
    public static QueryFilter ValidateFilter(QueryFilter? filter)
    {
        var term = NormalizeTerm(filter?.Term);
        if (term?.Length > 80) term = term[..80];

        var (from, to) = (filter?.From, filter?.To);
        if (from is not null && to is not null && from > to) (from, to) = (to, from);

        return new QueryFilter(from, to, term, Math.Clamp(filter?.MaxResults ?? 8, 1, 20));
    }

    public static IReadOnlyList<CrashRecord> Gather(IReadOnlyList<CrashRecord> records, QueryFilter filter) =>
        records
            .Where(r => filter.From is null || r.Date >= filter.From)
            .Where(r => filter.To is null || r.Date <= filter.To)
            .Where(r => filter.Term is null || Matches(r, filter.Term))
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.Id, StringComparer.Ordinal)
            .Take(filter.MaxResults ?? 8)
            .ToList();

    /// <summary>Models say "intersection crashes"; the corpus says "cross traffic (intersections only)".
    /// Dropping the filler words is what makes a whole-phrase match land.</summary>
    private static string? NormalizeTerm(string? value)
    {
        string[] filler = ["crash", "crashes", "record", "records"];
        var words = value?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(word => !filler.Contains(word, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        return words is { Length: > 0 } ? string.Join(' ', words) : null;
    }

    private static bool Matches(CrashRecord record, string term) =>
        record.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
        || record.Summary.Contains(term, StringComparison.OrdinalIgnoreCase)
        || record.Severity.Contains(term, StringComparison.OrdinalIgnoreCase);
}
