using System.Globalization;
using System.Text.RegularExpressions;

namespace Workshop.Core;

/// <summary>One raw claim as the model returned it, before any merging or numbering.</summary>
public sealed record ExtractedClaim(string SourceId, string Kind, string Value, string ExactQuote);

/// <summary>
/// Turns per-source model output into one ledger. Deterministic: the same extracted claims
/// always produce the same claim ids, ordering and grouping, so artifacts diff cleanly.
/// </summary>
public static partial class LedgerBuilder
{
    public static ClaimLedger Build(IEnumerable<ExtractedClaim> extracted)
    {
        var claims = extracted
            .Select(c => c with { Kind = NormalizeKind(c.Kind), Value = (c.Value ?? string.Empty).Trim() })
            .Where(c => c.Value.Length > 0)
            .GroupBy(c => (c.Kind, Value: c.Value.ToLowerInvariant()))
            .OrderBy(g => KindOrder(g.Key.Kind))
            .ThenBy(g => g.Key.Kind, StringComparer.Ordinal)
            .ThenBy(g => g.Key.Value, StringComparer.Ordinal)
            .Select((group, index) => new Claim(
                Id: $"C{index + 1:D3}",
                Kind: group.Key.Kind,
                Value: group.OrderBy(c => c.SourceId, StringComparer.Ordinal).First().Value,
                Evidence: [.. group
                    .Select(c => new EvidenceReference(c.SourceId, (c.ExactQuote ?? string.Empty).Trim()))
                    .DistinctBy(e => (e.SourceId, e.ExactQuote))
                    .OrderBy(e => e.SourceId, StringComparer.Ordinal)
                    .ThenBy(e => e.ExactQuote, StringComparer.Ordinal)]))
            .ToList();

        return new ClaimLedger(
            IncidentId: FirstValue(claims, ClaimKinds.IncidentId) ?? string.Empty,
            Severity: FirstValue(claims, ClaimKinds.Severity) ?? string.Empty,
            AffectedCustomers: FirstNumber(claims, ClaimKinds.AffectedCustomers),
            Claims: claims);
    }

    /// <summary>Accepts the small spelling drifts a 4B model makes ("Affected Customers", "incident-id").</summary>
    public static string NormalizeKind(string? kind) =>
        SeparatorPattern().Replace((kind ?? string.Empty).Trim().ToLowerInvariant(), "_");

    private static int KindOrder(string kind)
    {
        var index = ClaimKinds.All.ToList().IndexOf(kind);
        return index < 0 ? int.MaxValue : index;
    }

    private static string? FirstValue(IEnumerable<Claim> claims, string kind) =>
        claims.FirstOrDefault(c => c.Kind == kind)?.Value;

    private static int FirstNumber(IEnumerable<Claim> claims, string kind)
    {
        var value = FirstValue(claims, kind);
        if (value is null) return 0;
        var digits = DigitsPattern().Match(value);
        return digits.Success ? int.Parse(digits.Value, CultureInfo.InvariantCulture) : 0;
    }

    [GeneratedRegex(@"[\s\-]+")]
    private static partial Regex SeparatorPattern();

    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitsPattern();
}
