using System.Globalization;
using System.Text.RegularExpressions;

namespace Workshop.Core;

public sealed record TimelineEvent(DateTimeOffset Timestamp, string Description);

/// <summary>
/// Ground truth, parsed from the evidence pack by ordinary code. The model never sees this
/// and never influences it, which is what makes the verifier's comparison worth anything.
/// </summary>
public sealed record SourceFacts(
    string IncidentId,
    string Severity,
    int AffectedCustomers,
    DateTimeOffset StartedAt,
    DateTimeOffset ResolvedAt,
    IReadOnlyList<TimelineEvent> Timeline)
{
    public TimeSpan Duration => ResolvedAt - StartedAt;

    public IReadOnlyList<DateTimeOffset> KnownTimestamps =>
        [StartedAt, ResolvedAt, .. Timeline.Select(e => e.Timestamp)];

    /// <summary>The only descriptions of what happened that code can vouch for. R11 grades event claims against these.</summary>
    public IReadOnlyList<string> AllowedEventPhrases => [.. Timeline.Select(e => e.Description)];
}

public static partial class SourceFactsParser
{
    public static SourceFacts Parse(EvidenceStore store)
    {
        var status = store.Read("status.txt");
        var events = store.Read("events.csv");

        return new SourceFacts(
            IncidentId: Match(IncidentIdPattern(), status, "incident id"),
            Severity: Match(SeverityPattern(), status, "severity"),
            AffectedCustomers: int.Parse(Match(AffectedPattern(), status, "affected customers"), CultureInfo.InvariantCulture),
            StartedAt: ParseTimestamp(Match(StartedPattern(), status, "started timestamp")),
            ResolvedAt: ParseTimestamp(Match(ResolvedPattern(), status, "resolved timestamp")),
            Timeline: ParseTimeline(events));
    }

    public static DateTimeOffset ParseTimestamp(string value) =>
        DateTimeOffset.Parse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public static bool TryParseTimestamp(string value, out DateTimeOffset parsed) =>
        DateTimeOffset.TryParse(value?.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed);

    private static IReadOnlyList<TimelineEvent> ParseTimeline(string csv)
    {
        var timeline = new List<TimelineEvent>();
        foreach (var line in csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("timestamp,", StringComparison.OrdinalIgnoreCase)) continue;
            var separator = line.IndexOf(',');
            if (separator <= 0) continue;
            timeline.Add(new TimelineEvent(
                ParseTimestamp(line[..separator]),
                line[(separator + 1)..].Trim()));
        }
        return timeline;
    }

    private static string Match(Regex pattern, string text, string what)
    {
        var match = pattern.Match(text);
        return match.Success
            ? match.Groups[1].Value.Trim()
            : throw new EvidenceAccessException($"could not parse {what} from status.txt");
    }

    [GeneratedRegex(@"^(INC-\d+)", RegexOptions.Multiline)]
    private static partial Regex IncidentIdPattern();

    [GeneratedRegex(@"^Severity:\s*(\S+)", RegexOptions.Multiline)]
    private static partial Regex SeverityPattern();

    [GeneratedRegex(@"^Impact:\s*(\d+)\s+customers", RegexOptions.Multiline)]
    private static partial Regex AffectedPattern();

    [GeneratedRegex(@"^Started:\s*(\S+)", RegexOptions.Multiline)]
    private static partial Regex StartedPattern();

    [GeneratedRegex(@"^Resolved:\s*(\S+)", RegexOptions.Multiline)]
    private static partial Regex ResolvedPattern();
}
