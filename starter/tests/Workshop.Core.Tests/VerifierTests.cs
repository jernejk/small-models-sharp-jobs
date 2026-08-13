using Workshop.Core;
using Xunit;

namespace Workshop.Core.Tests;

public class VerifierTests
{
    private static ClaimLedger WithClaim(Claim claim) =>
        TestFixtures.CleanLedger() with { Claims = [claim] };

    private static IEnumerable<VerificationResult> For(string ruleId, ClaimLedger ledger) =>
        TestFixtures.Verify(ledger).Results.Where(r => r.RuleId == ruleId);

    [Fact]
    public void CleanLedgerHasNoFailures()
    {
        var report = TestFixtures.Verify(TestFixtures.CleanLedger());
        Assert.Equal(0, report.Failed);
        Assert.False(report.HasFailures);
    }

    [Fact]
    public void QuoteThatIsNotInTheSourceFails()
    {
        var ledger = WithClaim(new Claim("C001", ClaimKinds.Severity, "SEV-2",
            [new EvidenceReference("status.txt", "Severity: SEV-9 catastrophic")]));

        Assert.Contains(For(VerificationRules.QuotePresent, ledger), r => r.Status == VerificationStatus.Fail);
    }

    [Fact]
    public void QuoteSurvivesLineWrappingAndUnicodeNormalization()
    {
        var wrapped = "Impact:   7 customers\n   could not submit construction inspection forms.";
        var ledger = WithClaim(new Claim("C001", ClaimKinds.AffectedCustomers, "7",
            [new EvidenceReference("status.txt", wrapped)]));

        Assert.All(For(VerificationRules.QuotePresent, ledger), r => Assert.Equal(VerificationStatus.Pass, r.Status));
    }

    [Fact]
    public void SourceOutsideTheWhitelistFails()
    {
        var ledger = WithClaim(new Claim("C001", ClaimKinds.Event, "something",
            [new EvidenceReference("internal-notes.txt", "something")]));

        Assert.Contains(For(VerificationRules.SourceWhitelist, ledger), r => r.Status == VerificationStatus.Fail);
    }

    [Fact]
    public void ClaimWithNoEvidenceFails()
    {
        var ledger = WithClaim(new Claim("C001", ClaimKinds.Severity, "SEV-2", []));
        Assert.Contains(For(VerificationRules.QuotePresent, ledger), r => r.Status == VerificationStatus.Fail);
    }

    [Theory]
    [InlineData("8", VerificationStatus.Fail)]
    [InlineData("7", VerificationStatus.Pass)]
    [InlineData("seven", VerificationStatus.Fail)]
    public void AffectedCustomerCountIsCheckedAgainstSourceParsing(string value, VerificationStatus expected)
    {
        var ledger = WithClaim(new Claim("C001", ClaimKinds.AffectedCustomers, value,
            [new EvidenceReference("status.txt", "Impact: 7 customers could not submit construction inspection forms.")]));

        Assert.Contains(For(VerificationRules.AffectedCustomers, ledger), r => r.ClaimId == "C001" && r.Status == expected);
    }

    [Theory]
    [InlineData("2026-08-13T09:12:00+10:00", VerificationStatus.Pass)]
    [InlineData("2026-08-13T09:21:00+10:00", VerificationStatus.Pass)]
    [InlineData("2026-08-13T10:45:00+10:00", VerificationStatus.Fail)]
    [InlineData("this morning", VerificationStatus.Fail)]
    public void TimestampsAreCheckedAgainstSourceParsing(string value, VerificationStatus expected)
    {
        var ledger = WithClaim(new Claim("C001", ClaimKinds.Timestamp, value,
            [new EvidenceReference("status.txt", "Started: 2026-08-13T09:12:00+10:00")]));

        Assert.Contains(For(VerificationRules.Timestamp, ledger), r => r.Status == expected);
    }

    [Theory]
    [InlineData("27 minutes", VerificationStatus.Pass)]
    [InlineData("45 minutes", VerificationStatus.Fail)]
    public void DurationIsCheckedAgainstSourceParsing(string value, VerificationStatus expected)
    {
        var ledger = WithClaim(new Claim("C001", ClaimKinds.Duration, value,
            [new EvidenceReference("status.txt", "Started: 2026-08-13T09:12:00+10:00")]));

        Assert.Contains(For(VerificationRules.Duration, ledger), r => r.Status == expected);
    }

    [Fact]
    public void CausalLanguageIsUnverifiedNotPassed()
    {
        var ledger = WithClaim(new Claim("C001", ClaimKinds.Cause, "new billing system",
            [new EvidenceReference("customer-email.txt", "the new billing system")]));

        var results = For(VerificationRules.CauseUnverified, ledger).ToList();
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(VerificationStatus.Unverified, r.Status));
        Assert.DoesNotContain(results, r => r.Status == VerificationStatus.Pass);
    }

    /// <summary>The evasion R12 exists for: relabel a causal sentence as an event and it used to pass every rule.</summary>
    [Theory]
    [InlineData(ClaimKinds.Event)]
    [InlineData(ClaimKinds.Timestamp)]
    [InlineData(ClaimKinds.Duration)]
    public void CausalClaimDeclaredAsAnotherKindFailsAndIsStillUnverified(string kind)
    {
        var ledger = WithClaim(new Claim("C001", kind, DefectInjector.MislabelledCauseValue,
            [new EvidenceReference("customer-email.txt", "Our team believes the outage was caused by the new billing system.")]));

        Assert.Contains(For(VerificationRules.KindSemantics, ledger), r => r.Status == VerificationStatus.Fail);
        Assert.Contains(For(VerificationRules.CauseUnverified, ledger), r => r.Status == VerificationStatus.Unverified);
        Assert.DoesNotContain(For(VerificationRules.EventSupported, ledger), r => r.Status == VerificationStatus.Pass);
    }

    [Fact]
    public void MislabelledCauseNeverReachesVerifiedFacts()
    {
        var ledger = DefectInjector.Inject(TestFixtures.CleanLedger(), SeededDefect.MislabelledCause);
        var report = TestFixtures.Verify(ledger);
        var brief = BriefRenderer.Render(ledger, report, TestFixtures.Facts());

        Assert.DoesNotContain("| C903 |", BriefRenderer.Section(brief, "## Verified facts"), StringComparison.Ordinal);
        Assert.True(report.HasFailures);
    }

    /// <summary>A correctly declared cause is unverified, not a rule violation. Honest labelling must not be punished.</summary>
    [Fact]
    public void CorrectlyDeclaredCauseDoesNotTripTheKindRule()
    {
        var ledger = WithClaim(new Claim("C001", ClaimKinds.Cause, "new billing system",
            [new EvidenceReference("customer-email.txt", "Our team believes the outage was caused by the new billing system.")]));

        Assert.Empty(For(VerificationRules.KindSemantics, ledger));
        Assert.Contains(For(VerificationRules.CauseUnverified, ledger), r => r.Status == VerificationStatus.Unverified);
    }

    [Fact]
    public void EventInTheParsedLogPasses()
    {
        var ledger = WithClaim(new Claim("C001", ClaimKinds.Event, "routing rule replaced",
            [new EvidenceReference("events.csv", "2026-08-13T09:34:00+10:00,routing rule replaced")]));

        Assert.Contains(For(VerificationRules.EventSupported, ledger), r => r.Status == VerificationStatus.Pass);
    }

    /// <summary>Free text nobody can refute is not a failure and is not a fact. It is the third answer.</summary>
    [Fact]
    public void EventThatTheLogDoesNotContainIsUnverifiedNotPassed()
    {
        var ledger = WithClaim(new Claim("C001", ClaimKinds.Event, DefectInjector.UnsupportedEventValue,
            [new EvidenceReference("runbook.md", "Routing rules are cached for 10 minutes after replacement.")]));
        var report = TestFixtures.Verify(ledger);

        Assert.Contains(For(VerificationRules.EventSupported, ledger), r => r.Status == VerificationStatus.Unverified);
        Assert.DoesNotContain(report.Results, r => r.ClaimId == "C001" && r.Status == VerificationStatus.Fail);
        Assert.DoesNotContain("| C001 |",
            BriefRenderer.Section(BriefRenderer.Render(ledger, report, TestFixtures.Facts()), "## Verified facts"),
            StringComparison.Ordinal);
    }

    /// <summary>Both fragments are real; the sentence is not. Whole-file matching used to accept this.</summary>
    [Fact]
    public void QuoteSplicedFromTwoSourceLinesFails()
    {
        var ledger = WithClaim(new Claim("C001", ClaimKinds.Event, "submissions verified healthy",
            [new EvidenceReference("customer-email.txt", DefectInjector.SplicedQuoteText)]));

        Assert.Contains(For(VerificationRules.QuotePresent, ledger), r => r.Status == VerificationStatus.Fail);
    }

    [Fact]
    public void MissingRequiredKindFails()
    {
        var ledger = TestFixtures.CleanLedger() with
        {
            Claims = [.. TestFixtures.CleanLedger().Claims.Where(c => c.Kind != ClaimKinds.Severity)]
        };

        Assert.Contains(For(VerificationRules.RequiredClaims, ledger),
            r => r.Status == VerificationStatus.Fail && r.Detail.Contains(ClaimKinds.Severity, StringComparison.Ordinal));
    }

    [Fact]
    public void UnknownKindFails()
    {
        var ledger = WithClaim(new Claim("C001", "vibe", "good",
            [new EvidenceReference("status.txt", "INC-042 STATUS PAGE")]));

        Assert.Contains(For(VerificationRules.KnownKind, ledger), r => r.Status == VerificationStatus.Fail);
    }

    [Fact]
    public void LedgerHeaderIsCheckedIndependentlyOfClaims()
    {
        var ledger = TestFixtures.CleanLedger() with { AffectedCustomers = 9 };

        Assert.Contains(For(VerificationRules.AffectedCustomers, ledger),
            r => r.ClaimId == VerificationRules.LedgerScope && r.Status == VerificationStatus.Fail);
    }

    [Fact]
    public void EveryResultCarriesClaimIdRuleIdStatusAndDetail()
    {
        foreach (var result in TestFixtures.Verify(TestFixtures.CleanLedger()).Results)
        {
            Assert.False(string.IsNullOrWhiteSpace(result.ClaimId));
            Assert.False(string.IsNullOrWhiteSpace(result.RuleId));
            Assert.False(string.IsNullOrWhiteSpace(result.Detail));
            Assert.True(Enum.IsDefined(result.Status));
        }
    }
}
