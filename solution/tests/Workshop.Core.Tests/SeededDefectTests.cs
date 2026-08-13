using Workshop.Core;
using Xunit;

namespace Workshop.Core.Tests;

public class SeededDefectTests
{
    /// <summary>The gate contract: every defect, not just the three that fail loudly.</summary>
    public static TheoryData<SeededDefect> Defects
    {
        get
        {
            var data = new TheoryData<SeededDefect>();
            foreach (var defect in DefectInjector.All) data.Add(defect);
            return data;
        }
    }

    private static DefectOutcome Evaluate(SeededDefect defect, ClaimLedger ledger) =>
        DefectInjector.Evaluate(defect, ledger, TestFixtures.Facts(), TestFixtures.Store());

    /// <summary>Catching a defect for the wrong reason is not catching it, and one pass is not a result.</summary>
    [Theory]
    [MemberData(nameof(Defects))]
    public void DefectIsRejectedForTheIntendedRuleThreeTimesOutOfThree(SeededDefect defect)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var outcome = Evaluate(defect, TestFixtures.CleanLedger());

            Assert.True(outcome.RuleFiredAsIntended,
                $"attempt {attempt}: wanted {outcome.Expected.RuleId}={outcome.Expected.Status}, "
                + $"got failures [{string.Join(",", outcome.FailedRuleIds)}]");
            Assert.True(outcome.KeptOutOfVerifiedFacts,
                $"attempt {attempt}: {string.Join(",", outcome.TouchedClaimIds)} reached verified facts");
            Assert.NotEmpty(outcome.TouchedClaimIds);
        }
    }

    /// <summary>Verification is deterministic, so three identical verdicts is the property under test.</summary>
    [Theory]
    [MemberData(nameof(Defects))]
    public void RepeatedInjectionProducesByteIdenticalArtifacts(SeededDefect defect)
    {
        var first = Evaluate(defect, TestFixtures.CleanLedger());
        var second = Evaluate(defect, TestFixtures.CleanLedger());

        Assert.Equal(WorkshopJson.Serialize(first.Ledger), WorkshopJson.Serialize(second.Ledger));
        Assert.Equal(WorkshopJson.Serialize(first.Report), WorkshopJson.Serialize(second.Report));
        Assert.Equal(first.Brief, second.Brief);
    }

    /// <summary>A defect that trips a second rule teaches the wrong lesson in the break-it demo.</summary>
    [Theory]
    [MemberData(nameof(Defects))]
    public void DefectFailsOnAtMostItsIntendedRule(SeededDefect defect)
    {
        var outcome = Evaluate(defect, TestFixtures.CleanLedger());
        var expectedFailures = outcome.Expected.Status == VerificationStatus.Fail
            ? new[] { outcome.Expected.RuleId }
            : [];

        Assert.Equal(expectedFailures, outcome.FailedRuleIds);
    }

    [Theory]
    [MemberData(nameof(Defects))]
    public void DefectIsInjectableIntoALedgerThatLacksTheTargetKind(SeededDefect defect)
    {
        var sparse = TestFixtures.CleanLedger() with
        {
            Claims = [.. TestFixtures.CleanLedger().Claims.Where(c => c.Kind == ClaimKinds.IncidentId)]
        };

        Assert.True(Evaluate(defect, sparse).RuleFiredAsIntended);
    }

    [Fact]
    public void NoDefectLeavesTheLedgerUntouched()
    {
        Assert.Equal(
            WorkshopJson.Serialize(TestFixtures.CleanLedger()),
            WorkshopJson.Serialize(DefectInjector.Inject(TestFixtures.CleanLedger(), SeededDefect.None)));
        Assert.Empty(DefectInjector.TouchedClaimIds(TestFixtures.CleanLedger(), TestFixtures.CleanLedger()));
    }

    [Fact]
    public void PhantomSourceDoesNotAlsoTripTheQuoteRule()
    {
        var outcome = Evaluate(SeededDefect.PhantomSource, TestFixtures.CleanLedger());

        Assert.DoesNotContain(outcome.Report.Results,
            r => r.ClaimId == "C900" && r.RuleId == VerificationRules.QuotePresent && r.Status == VerificationStatus.Fail);
    }

    /// <summary>An unverified defect is still rejected: it is reported, never asserted.</summary>
    [Fact]
    public void UnsupportedEventIsShownButNotVerified()
    {
        var outcome = Evaluate(SeededDefect.UnsupportedEvent, TestFixtures.CleanLedger());

        Assert.False(outcome.Report.HasFailures);
        Assert.Contains(DefectInjector.UnsupportedEventValue,
            BriefRenderer.Section(outcome.Brief, "## Shown but not verified"), StringComparison.Ordinal);
        Assert.True(outcome.Rejected);
    }

    [Fact]
    public void EveryDefectHasAnExpectationAndAnInjector()
    {
        foreach (var defect in Enum.GetValues<SeededDefect>().Where(d => d != SeededDefect.None))
        {
            Assert.Contains(defect, DefectInjector.All);
            Assert.NotEmpty(DefectInjector.Expected(defect).RuleId);
        }
    }
}
