using System.Globalization;
using System.Text.RegularExpressions;

namespace Workshop.Core;

/// <summary>
/// Ordinary code, no model. Every rule compares a model-produced claim against either the
/// evidence text or facts parsed independently by <see cref="SourceFactsParser"/>.
/// </summary>
public static partial class Verifier
{
    public static VerificationReport Verify(ClaimLedger ledger, SourceFacts facts, EvidenceStore store)
    {
        var results = new List<VerificationResult>();

        results.Add(CompareText(VerificationRules.IncidentId, VerificationRules.LedgerScope,
            ledger.IncidentId, facts.IncidentId, "ledger incident id"));
        results.Add(CompareText(VerificationRules.Severity, VerificationRules.LedgerScope,
            ledger.Severity, facts.Severity, "ledger severity"));
        results.Add(CompareText(VerificationRules.AffectedCustomers, VerificationRules.LedgerScope,
            ledger.AffectedCustomers.ToString(CultureInfo.InvariantCulture),
            facts.AffectedCustomers.ToString(CultureInfo.InvariantCulture), "ledger affected customers"));

        foreach (var claim in ledger.Claims.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            results.AddRange(VerifyClaim(claim, facts, store));
        }

        results.Add(CheckRequiredClaims(ledger));

        return VerificationReport.From(ledger.IncidentId, results);
    }

    private static IEnumerable<VerificationResult> VerifyClaim(Claim claim, SourceFacts facts, EvidenceStore store)
    {
        if (!ClaimKinds.IsKnown(claim.Kind))
        {
            yield return new VerificationResult(claim.Id, VerificationRules.KnownKind, VerificationStatus.Fail,
                $"'{claim.Kind}' is not one of: {string.Join(", ", ClaimKinds.All)}");
            yield break;
        }

        yield return new VerificationResult(claim.Id, VerificationRules.KnownKind, VerificationStatus.Pass,
            $"kind '{claim.Kind}' is recognised");

        if (claim.Evidence.Count == 0)
        {
            yield return new VerificationResult(claim.Id, VerificationRules.QuotePresent, VerificationStatus.Fail,
                "claim cites no evidence");
        }

        foreach (var reference in claim.Evidence)
        {
            if (!EvidenceStore.IsWhitelisted(reference.SourceId))
            {
                yield return new VerificationResult(claim.Id, VerificationRules.SourceWhitelist, VerificationStatus.Fail,
                    $"'{reference.SourceId}' is not in the evidence whitelist");
                continue;
            }

            yield return new VerificationResult(claim.Id, VerificationRules.SourceWhitelist, VerificationStatus.Pass,
                $"'{reference.SourceId}' is a known evidence source");

            yield return CheckQuote(claim, reference, store);
        }

        foreach (var result in CheckSemantics(claim, facts))
        {
            yield return result;
        }

        foreach (var result in CheckValue(claim, facts))
        {
            yield return result;
        }
    }

    /// <summary>
    /// Meaning checks that do not trust the declared kind. A model that labels a causal sentence
    /// as an event would otherwise reach the brief with nothing but a real quote behind it, which
    /// is exactly the evasion R9 exists to prevent.
    /// </summary>
    private static IEnumerable<VerificationResult> CheckSemantics(Claim claim, SourceFacts facts)
    {
        var causal = ClaimSemantics.DetectCause(claim);
        var declaredCause = string.Equals(claim.Kind, ClaimKinds.Cause, StringComparison.Ordinal);

        if (causal is not null && !declaredCause)
        {
            yield return new VerificationResult(claim.Id, VerificationRules.KindSemantics, VerificationStatus.Fail,
                $"declared kind '{claim.Kind}' but the {causal.Where} asserts causation (\"{causal.Marker}\"); "
                + $"a claim about why the incident happened must be declared as '{ClaimKinds.Cause}'");
        }

        if (causal is not null || declaredCause)
        {
            yield return new VerificationResult(claim.Id, VerificationRules.CauseUnverified, VerificationStatus.Unverified,
                causal is null
                    ? "cause cannot be established from the evidence pack by deterministic checks"
                    : $"causal wording (\"{causal.Marker}\") in the {causal.Where} cannot be established from the evidence pack by deterministic checks");
            yield break;
        }

        if (string.Equals(claim.Kind, ClaimKinds.Event, StringComparison.Ordinal))
        {
            yield return ClaimSemantics.IsSupportedEvent(claim.Value, facts)
                ? new VerificationResult(claim.Id, VerificationRules.EventSupported, VerificationStatus.Pass,
                    $"'{claim.Value}' is one of the events code parsed from events.csv")
                : new VerificationResult(claim.Id, VerificationRules.EventSupported, VerificationStatus.Unverified,
                    $"'{claim.Value}' is not one of the events code parsed from events.csv "
                    + $"({string.Join("; ", facts.AllowedEventPhrases)})");
        }
    }

    private static VerificationResult CheckQuote(Claim claim, EvidenceReference reference, EvidenceStore store)
    {
        var sourceText = store.Read(reference.SourceId);
        return TextNormalization.ContainsQuote(sourceText, reference.ExactQuote)
            ? new VerificationResult(claim.Id, VerificationRules.QuotePresent, VerificationStatus.Pass,
                $"quote found in {reference.SourceId}")
            : new VerificationResult(claim.Id, VerificationRules.QuotePresent, VerificationStatus.Fail,
                $"quote not found in {reference.SourceId}: \"{reference.ExactQuote}\"");
    }

    private static IEnumerable<VerificationResult> CheckValue(Claim claim, SourceFacts facts)
    {
        switch (claim.Kind)
        {
            case ClaimKinds.IncidentId:
                yield return CompareText(VerificationRules.IncidentId, claim.Id, claim.Value, facts.IncidentId, "incident id");
                break;

            case ClaimKinds.Severity:
                yield return CompareText(VerificationRules.Severity, claim.Id, claim.Value, facts.Severity, "severity");
                break;

            case ClaimKinds.AffectedCustomers:
                yield return CompareNumber(claim, facts.AffectedCustomers);
                break;

            case ClaimKinds.Timestamp:
                yield return CompareTimestamp(claim, facts);
                break;

            case ClaimKinds.Duration:
                yield return CompareDuration(claim, facts);
                break;
        }
    }

    private static VerificationResult CompareText(string ruleId, string claimId, string actual, string expected, string what) =>
        string.Equals(actual?.Trim(), expected, StringComparison.OrdinalIgnoreCase)
            ? new VerificationResult(claimId, ruleId, VerificationStatus.Pass, $"{what} matches source parsing ({expected})")
            : new VerificationResult(claimId, ruleId, VerificationStatus.Fail,
                $"{what} is '{actual}' but source parsing gives '{expected}'");

    private static VerificationResult CompareNumber(Claim claim, int expected)
    {
        var digits = DigitsPattern().Match(claim.Value ?? string.Empty);
        if (!digits.Success)
        {
            return new VerificationResult(claim.Id, VerificationRules.AffectedCustomers, VerificationStatus.Fail,
                $"no number in '{claim.Value}'");
        }

        var actual = int.Parse(digits.Value, CultureInfo.InvariantCulture);
        return actual == expected
            ? new VerificationResult(claim.Id, VerificationRules.AffectedCustomers, VerificationStatus.Pass,
                $"affected customers matches source parsing ({expected})")
            : new VerificationResult(claim.Id, VerificationRules.AffectedCustomers, VerificationStatus.Fail,
                $"affected customers is {actual} but source parsing gives {expected}");
    }

    private static VerificationResult CompareTimestamp(Claim claim, SourceFacts facts)
    {
        if (!SourceFactsParser.TryParseTimestamp(claim.Value ?? string.Empty, out var parsed))
        {
            return new VerificationResult(claim.Id, VerificationRules.Timestamp, VerificationStatus.Fail,
                $"'{claim.Value}' is not a parsable timestamp");
        }

        return facts.KnownTimestamps.Any(known => known == parsed)
            ? new VerificationResult(claim.Id, VerificationRules.Timestamp, VerificationStatus.Pass,
                $"{parsed:O} matches a timestamp parsed from the evidence")
            : new VerificationResult(claim.Id, VerificationRules.Timestamp, VerificationStatus.Fail,
                $"{parsed:O} is not a timestamp found by source parsing");
    }

    private static VerificationResult CompareDuration(Claim claim, SourceFacts facts)
    {
        var digits = DigitsPattern().Match(claim.Value ?? string.Empty);
        if (!digits.Success)
        {
            return new VerificationResult(claim.Id, VerificationRules.Duration, VerificationStatus.Fail,
                $"no minute count in '{claim.Value}'");
        }

        var actual = int.Parse(digits.Value, CultureInfo.InvariantCulture);
        var expected = (int)facts.Duration.TotalMinutes;
        return actual == expected
            ? new VerificationResult(claim.Id, VerificationRules.Duration, VerificationStatus.Pass,
                $"duration matches source parsing ({expected} minutes)")
            : new VerificationResult(claim.Id, VerificationRules.Duration, VerificationStatus.Fail,
                $"duration is {actual} minutes but source parsing gives {expected}");
    }

    private static VerificationResult CheckRequiredClaims(ClaimLedger ledger)
    {
        var present = ledger.Claims.Select(c => c.Kind).ToHashSet(StringComparer.Ordinal);
        var missing = ClaimKinds.Required.Where(kind => !present.Contains(kind)).ToList();

        return missing.Count == 0
            ? new VerificationResult(VerificationRules.LedgerScope, VerificationRules.RequiredClaims, VerificationStatus.Pass,
                $"all required kinds present: {string.Join(", ", ClaimKinds.Required)}")
            : new VerificationResult(VerificationRules.LedgerScope, VerificationRules.RequiredClaims, VerificationStatus.Fail,
                $"missing required claim kinds: {string.Join(", ", missing)}");
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitsPattern();
}
