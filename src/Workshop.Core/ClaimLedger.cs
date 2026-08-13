namespace Workshop.Core;

public sealed record ClaimLedger(
    string IncidentId,
    string Severity,
    int AffectedCustomers,
    IReadOnlyList<Claim> Claims);

public sealed record Claim(
    string Id,
    string Kind,
    string Value,
    IReadOnlyList<EvidenceReference> Evidence);

public sealed record EvidenceReference(
    string SourceId,
    string ExactQuote);

public static class ClaimKinds
{
    public const string IncidentId = "incident_id";
    public const string Severity = "severity";
    public const string AffectedCustomers = "affected_customers";
    public const string Timestamp = "timestamp";
    public const string Duration = "duration";
    public const string Event = "event";
    public const string Cause = "cause";

    public static readonly IReadOnlyList<string> All =
    [
        IncidentId, Severity, AffectedCustomers, Timestamp, Duration, Event, Cause
    ];

    /// <summary>Kinds a brief is useless without. Missing ones fail R8.</summary>
    public static readonly IReadOnlyList<string> Required =
    [
        IncidentId, Severity, AffectedCustomers
    ];

    public static bool IsKnown(string kind) => All.Contains(kind, StringComparer.Ordinal);
}
