using System.Text.Json.Serialization;

namespace Workshop.Core;

[JsonConverter(typeof(JsonStringEnumConverter<VerificationStatus>))]
public enum VerificationStatus
{
    [JsonStringEnumMemberName("PASS")] Pass,
    [JsonStringEnumMemberName("FAIL")] Fail,
    [JsonStringEnumMemberName("UNVERIFIED")] Unverified
}

public sealed record VerificationResult(
    string ClaimId,
    string RuleId,
    VerificationStatus Status,
    string Detail);

public sealed record VerificationReport(
    string IncidentId,
    int Passed,
    int Failed,
    int Unverified,
    IReadOnlyList<VerificationResult> Results)
{
    public bool HasFailures => Failed > 0;

    public IReadOnlySet<string> FailedClaimIds =>
        Results.Where(r => r.Status == VerificationStatus.Fail).Select(r => r.ClaimId).ToHashSet(StringComparer.Ordinal);

    public IReadOnlySet<string> UnverifiedClaimIds =>
        Results.Where(r => r.Status == VerificationStatus.Unverified).Select(r => r.ClaimId).ToHashSet(StringComparer.Ordinal);

    public IReadOnlyList<string> RuleIdsWithStatus(VerificationStatus status) =>
        [.. Results.Where(r => r.Status == status).Select(r => r.RuleId).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    public static VerificationReport From(string incidentId, IReadOnlyList<VerificationResult> results) => new(
        incidentId,
        results.Count(r => r.Status == VerificationStatus.Pass),
        results.Count(r => r.Status == VerificationStatus.Fail),
        results.Count(r => r.Status == VerificationStatus.Unverified),
        results);
}

public static class VerificationRules
{
    public const string SourceWhitelist = "R1-SOURCE-WHITELIST";
    public const string QuotePresent = "R2-QUOTE-PRESENT";
    public const string IncidentId = "R3-INCIDENT-ID";
    public const string Severity = "R4-SEVERITY";
    public const string AffectedCustomers = "R5-AFFECTED-CUSTOMERS";
    public const string Timestamp = "R6-TIMESTAMP";
    public const string Duration = "R7-DURATION";
    public const string RequiredClaims = "R8-REQUIRED-CLAIMS";
    public const string CauseUnverified = "R9-CAUSE-UNVERIFIED";
    public const string KnownKind = "R10-KNOWN-KIND";
    public const string EventSupported = "R11-EVENT-SUPPORTED";
    public const string KindSemantics = "R12-KIND-SEMANTICS";

    /// <summary>Used as the claim id for checks that apply to the ledger header rather than one claim.</summary>
    public const string LedgerScope = "(ledger)";
}
