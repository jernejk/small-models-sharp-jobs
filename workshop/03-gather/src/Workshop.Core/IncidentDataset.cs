using System.Text.Json;

namespace Workshop.Core;

/// <summary>
/// The deliberately boring boundary used by the "Gather" workshop step.  It is not an agent and
/// it never accepts a path: a caller supplies an approved data file and a small query.
/// </summary>
public sealed record IncidentRecord(
    string Id,
    DateOnly Date,
    string Title,
    string Summary,
    string Severity,
    string SourceReference);

public sealed record IncidentQuery(DateOnly? From, DateOnly? To, string? Term, int MaxResults = 8);

/// <summary>Untrusted model interpretation of a question. Code validates it before filtering local records.</summary>
public sealed record QueryFilter(DateOnly? From, DateOnly? To, string? Term, int? MaxResults);

public sealed record EvidencePack(IReadOnlyList<IncidentRecord> Records)
{
    public bool IsEmpty => Records.Count == 0;
}

public static class IncidentDataset
{
    public static IncidentQuery ValidateFilter(QueryFilter? filter)
    {
        var term = NormalizeTerm(filter?.Term);
        if (string.IsNullOrWhiteSpace(term)) term = null;
        if (term?.Length > 80) term = term[..80];
        var from = filter?.From;
        var to = filter?.To;
        if (from is not null && to is not null && from > to) (from, to) = (to, from);
        return new IncidentQuery(from, to, term, Math.Clamp(filter?.MaxResults ?? 8, 1, 20));
    }

    public static IReadOnlyList<IncidentRecord> Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("A dataset file is required.", nameof(filePath));
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<IncidentRecord>>(json, WorkshopJson.Options)
            ?? throw new InvalidDataException("Incident dataset was empty or invalid JSON.");
    }

    public static EvidencePack Gather(IEnumerable<IncidentRecord> records, IncidentQuery query)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(query);
        var term = query.Term?.Trim();
        var take = Math.Clamp(query.MaxResults, 1, 20);
        var selected = records
            .Where(record => query.From is null || record.Date >= query.From)
            .Where(record => query.To is null || record.Date <= query.To)
            .Where(record => string.IsNullOrEmpty(term) || Matches(record, term))
            .OrderByDescending(record => record.Date)
            .ThenBy(record => record.Id, StringComparer.Ordinal)
            .Take(take)
            .ToList();

        return new EvidencePack(selected);
    }

    private static bool Matches(IncidentRecord record, string term) =>
        record.Id.Contains(term, StringComparison.OrdinalIgnoreCase)
        || record.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
        || record.Summary.Contains(term, StringComparison.OrdinalIgnoreCase)
        || record.Severity.Contains(term, StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeTerm(string? value)
    {
        var words = value?.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(word => !string.Equals(word, "crash", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(word, "crashes", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(word, "record", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(word, "records", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return words is { Length: > 0 } ? string.Join(' ', words) : null;
    }
}
