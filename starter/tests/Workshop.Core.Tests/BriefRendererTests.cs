using Workshop.Core;
using Xunit;

namespace Workshop.Core.Tests;

public class BriefRendererTests
{
    private static string Render(ClaimLedger ledger) =>
        BriefRenderer.Render(ledger, TestFixtures.Verify(ledger), TestFixtures.Facts());

    [Fact]
    public void IdenticalInputProducesIdenticalBytes()
    {
        var ledger = TestFixtures.CleanLedger();
        Assert.Equal(Render(ledger), Render(ledger));
    }

    [Fact]
    public void VerifiedClaimsAppearWithCitations()
    {
        var brief = Render(TestFixtures.CleanLedger());
        Assert.Contains("| C002 | severity | SEV-2 | `status.txt`: \"Severity: SEV-2\" |", brief, StringComparison.Ordinal);
    }

    [Fact]
    public void FailedClaimIsNeverPresentedAsVerifiedProse()
    {
        var ledger = DefectInjector.Inject(TestFixtures.CleanLedger(), SeededDefect.AlteredNumber);
        var brief = Render(ledger);

        var verifiedSection = Section(brief, "## Verified facts");
        Assert.DoesNotContain("affected_customers", verifiedSection, StringComparison.Ordinal);
        Assert.Contains("Excluded by verification", brief, StringComparison.Ordinal);
    }

    [Fact]
    public void FailuresAreCountedAndNamedRatherThanDropped()
    {
        var ledger = DefectInjector.Inject(TestFixtures.CleanLedger(), SeededDefect.PhantomSource);
        var excluded = Section(Render(ledger), "## Excluded by verification");

        Assert.Contains("item(s) failed verification", excluded, StringComparison.Ordinal);
        Assert.Contains(VerificationRules.SourceWhitelist, excluded, StringComparison.Ordinal);
    }

    [Fact]
    public void UnverifiedClaimIsVisibleAndNotInVerifiedFacts()
    {
        var brief = Render(TestFixtures.CleanLedger());

        Assert.DoesNotContain("new billing system", Section(brief, "## Verified facts"), StringComparison.Ordinal);
        var shown = Section(brief, "## Shown but not verified");
        Assert.Contains("new billing system", shown, StringComparison.Ordinal);
        Assert.Contains(VerificationRules.CauseUnverified, shown, StringComparison.Ordinal);
    }

    [Fact]
    public void TimelineComesFromDeterministicParsingNotTheLedger()
    {
        var brief = Render(TestFixtures.CleanLedger() with { Claims = [] });

        Assert.Contains("stale routing rule identified", brief, StringComparison.Ordinal);
        Assert.Contains("Duration from source parsing: 27 minutes", brief, StringComparison.Ordinal);
    }

    [Fact]
    public void HeaderRefusesTheIncidentIdWhenTheLedgerHeaderFailed()
    {
        var brief = Render(TestFixtures.CleanLedger() with { IncidentId = "INC-999" });
        Assert.StartsWith("# Incident brief: unverified incident", brief, StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryCountsMatchTheReport()
    {
        var ledger = TestFixtures.CleanLedger();
        var report = TestFixtures.Verify(ledger);
        var brief = BriefRenderer.Render(ledger, report, TestFixtures.Facts());

        Assert.Contains($"- passed: {report.Passed}", brief, StringComparison.Ordinal);
        Assert.Contains($"- failed: {report.Failed}", brief, StringComparison.Ordinal);
        Assert.Contains($"- unverified: {report.Unverified}", brief, StringComparison.Ordinal);
    }

    [Fact]
    public void UsesUnixLineEndingsOnly() =>
        Assert.DoesNotContain('\r', Render(TestFixtures.CleanLedger()));

    private static string Section(string brief, string heading)
    {
        var start = brief.IndexOf(heading, StringComparison.Ordinal);
        Assert.True(start >= 0, $"missing section {heading}");
        var next = brief.IndexOf("\n## ", start + heading.Length, StringComparison.Ordinal);
        return next < 0 ? brief[start..] : brief[start..next];
    }
}
