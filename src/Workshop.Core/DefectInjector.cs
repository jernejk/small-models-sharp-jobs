namespace Workshop.Core;

public enum SeededDefect
{
    None,
    PhantomSource,
    AlteredNumber,
    AlteredTimestamp,
    MislabelledCause,
    UnsupportedEvent,
    SplicedQuote
}

/// <summary>What a defect must do to the report. UNVERIFIED is a rejection too: it keeps the claim out of the brief's verified facts.</summary>
public sealed record DefectExpectation(string RuleId, VerificationStatus Status);

/// <summary>The verdict for one injection: the rule fired as intended, and nothing the defect touched reached the brief.</summary>
public sealed record DefectOutcome(
    SeededDefect Defect,
    DefectExpectation Expected,
    bool RuleFiredAsIntended,
    bool KeptOutOfVerifiedFacts,
    IReadOnlyList<string> TouchedClaimIds,
    IReadOnlyList<string> FailedRuleIds,
    ClaimLedger Ledger,
    VerificationReport Report,
    string Brief)
{
    public bool Rejected => RuleFiredAsIntended && KeptOutOfVerifiedFacts;
}

/// <summary>
/// Corrupts a ledger the way a careless or hallucinating model would, so the workshop can watch
/// the verifier catch it. Each defect is designed to trip exactly one rule.
/// </summary>
public static class DefectInjector
{
    public const string PhantomSourceId = "internal-notes.txt";
    public const string UnknownTimestamp = "2026-08-13T10:45:00+10:00";
    public const int WrongCustomerCount = 9;

    /// <summary>A real sentence from a real source, relabelled so it would slip past a kind-trusting verifier.</summary>
    public const string MislabelledCauseValue = "the outage was caused by the new billing system";

    /// <summary>Plausible incident wording that appears in no event log line.</summary>
    public const string UnsupportedEventValue = "engineers restarted the billing service";

    /// <summary>Two real fragments from two different lines, welded into a quote that was never written.</summary>
    public const string SplicedQuoteText = "Cannot submit inspection forms Our crews could not submit inspection forms at several sites.";

    public static readonly IReadOnlyList<SeededDefect> All =
    [
        SeededDefect.PhantomSource, SeededDefect.AlteredNumber, SeededDefect.AlteredTimestamp,
        SeededDefect.MislabelledCause, SeededDefect.UnsupportedEvent, SeededDefect.SplicedQuote
    ];

    public static ClaimLedger Inject(ClaimLedger ledger, SeededDefect defect) => defect switch
    {
        SeededDefect.None => ledger,
        SeededDefect.PhantomSource => InjectPhantomSource(ledger),
        SeededDefect.AlteredNumber => InjectAlteredNumber(ledger),
        SeededDefect.AlteredTimestamp => InjectAlteredTimestamp(ledger),
        SeededDefect.MislabelledCause => InjectMislabelledCause(ledger),
        SeededDefect.UnsupportedEvent => InjectUnsupportedEvent(ledger),
        SeededDefect.SplicedQuote => InjectSplicedQuote(ledger),
        _ => throw new ArgumentOutOfRangeException(nameof(defect))
    };

    public static DefectExpectation Expected(SeededDefect defect) => defect switch
    {
        SeededDefect.PhantomSource => new(VerificationRules.SourceWhitelist, VerificationStatus.Fail),
        SeededDefect.AlteredNumber => new(VerificationRules.AffectedCustomers, VerificationStatus.Fail),
        SeededDefect.AlteredTimestamp => new(VerificationRules.Timestamp, VerificationStatus.Fail),
        SeededDefect.MislabelledCause => new(VerificationRules.KindSemantics, VerificationStatus.Fail),
        SeededDefect.UnsupportedEvent => new(VerificationRules.EventSupported, VerificationStatus.Unverified),
        SeededDefect.SplicedQuote => new(VerificationRules.QuotePresent, VerificationStatus.Fail),
        _ => throw new ArgumentOutOfRangeException(nameof(defect))
    };

    public static string ExpectedRuleId(SeededDefect defect) => Expected(defect).RuleId;

    /// <summary>
    /// Inject, verify and render, then judge on the property that actually matters: the intended
    /// rule fired with the intended status, and no claim the defect touched reached verified facts.
    /// </summary>
    public static DefectOutcome Evaluate(SeededDefect defect, ClaimLedger clean, SourceFacts facts, EvidenceStore store)
    {
        var injected = Inject(clean, defect);
        var report = Verifier.Verify(injected, facts, store);
        var brief = BriefRenderer.Render(injected, report, facts);
        var expected = Expected(defect);
        var touched = TouchedClaimIds(clean, injected);
        var verifiedFacts = BriefRenderer.Section(brief, "## Verified facts");

        return new DefectOutcome(
            defect,
            expected,
            report.Results.Any(r => r.RuleId == expected.RuleId && r.Status == expected.Status),
            touched.All(id => !verifiedFacts.Contains($"| {id} |", StringComparison.Ordinal)),
            touched,
            report.RuleIdsWithStatus(VerificationStatus.Fail),
            injected, report, brief);
    }

    /// <summary>Claim ids the injection added or changed, compared by serialized content so nothing subtle is missed.</summary>
    public static IReadOnlyList<string> TouchedClaimIds(ClaimLedger clean, ClaimLedger injected)
    {
        var before = clean.Claims.ToDictionary(c => c.Id, WorkshopJson.Serialize, StringComparer.Ordinal);
        return
        [
            .. injected.Claims
                .Where(c => !before.TryGetValue(c.Id, out var original) || original != WorkshopJson.Serialize(c))
                .Select(c => c.Id)
                .Order(StringComparer.Ordinal)
        ];
    }

    private static ClaimLedger InjectPhantomSource(ClaimLedger ledger) => ledger with
    {
        Claims = [.. ledger.Claims, new Claim(
            Id: "C900",
            Kind: ClaimKinds.Event,
            Value: "routing rule replaced",
            Evidence: [new EvidenceReference(PhantomSourceId, "routing rule replaced")])]
    };

    private static ClaimLedger InjectAlteredNumber(ClaimLedger ledger)
    {
        var existing = ledger.Claims.FirstOrDefault(c => c.Kind == ClaimKinds.AffectedCustomers);
        var claims = existing is null
            ? (IReadOnlyList<Claim>)[.. ledger.Claims, new Claim(
                Id: "C901",
                Kind: ClaimKinds.AffectedCustomers,
                Value: WrongCustomerCount.ToString(),
                Evidence: [new EvidenceReference("status.txt", "Impact: 7 customers could not submit construction inspection forms.")])]
            : [.. ledger.Claims.Select(c => c.Kind == ClaimKinds.AffectedCustomers
                ? c with { Value = WrongCustomerCount.ToString() }
                : c)];

        return ledger with { AffectedCustomers = WrongCustomerCount, Claims = claims };
    }

    private static ClaimLedger InjectAlteredTimestamp(ClaimLedger ledger)
    {
        var existing = ledger.Claims.FirstOrDefault(c => c.Kind == ClaimKinds.Timestamp);
        IReadOnlyList<Claim> claims = existing is null
            ? [.. ledger.Claims, new Claim(
                Id: "C902",
                Kind: ClaimKinds.Timestamp,
                Value: UnknownTimestamp,
                Evidence: [new EvidenceReference("status.txt", "Resolved: 2026-08-13T09:39:00+10:00")])]
            : [.. ledger.Claims.Select(c => c.Id == existing.Id
                ? c with { Value = UnknownTimestamp }
                : c)];

        return ledger with { Claims = claims };
    }

    private static ClaimLedger InjectMislabelledCause(ClaimLedger ledger) => ledger with
    {
        Claims = [.. ledger.Claims, new Claim(
            Id: "C903",
            Kind: ClaimKinds.Event,
            Value: MislabelledCauseValue,
            Evidence: [new EvidenceReference("customer-email.txt", "Our team believes the outage was caused by the new billing system.")])]
    };

    private static ClaimLedger InjectUnsupportedEvent(ClaimLedger ledger) => ledger with
    {
        Claims = [.. ledger.Claims, new Claim(
            Id: "C904",
            Kind: ClaimKinds.Event,
            Value: UnsupportedEventValue,
            Evidence: [new EvidenceReference("runbook.md", "Routing rules are cached for 10 minutes after replacement.")])]
    };

    private static ClaimLedger InjectSplicedQuote(ClaimLedger ledger) => ledger with
    {
        Claims = [.. ledger.Claims, new Claim(
            Id: "C905",
            Kind: ClaimKinds.Event,
            Value: "submissions verified healthy",
            Evidence: [new EvidenceReference("customer-email.txt", SplicedQuoteText)])]
    };
}
