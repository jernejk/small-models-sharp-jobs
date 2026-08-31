using System.Globalization;
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
    public static CrashQuery ValidateFilter(QueryFilter? filter)
    {
        var term = NormalizeTerm(filter?.Term);
        if (term?.Length > 80) term = term[..80];

        var from = ParseDate(filter?.From);
        var to = ParseDate(filter?.To);
        if (from is not null && to is not null && from > to) (from, to) = (to, from);

        return new CrashQuery(from, to, term, Math.Clamp(filter?.MaxResults ?? 8, 1, 20));
    }

    /// <summary>Anything that is not a real date becomes "no date filter" rather than an exception.</summary>
    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    public static IReadOnlyList<CrashRecord> Gather(IReadOnlyList<CrashRecord> records, CrashQuery filter) =>
        records
            .Where(r => filter.From is null || r.Date >= filter.From)
            .Where(r => filter.To is null || r.Date <= filter.To)
            .Where(r => filter.Term is null || Matches(r, filter.Term))
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.Id, StringComparer.Ordinal)
            .Take(filter.MaxResults)
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
