using Workshop.Core;
using Xunit;

namespace Workshop.Core.Tests;

public class LedgerBuilderTests
{
    private static readonly ExtractedClaim[] Sample =
    [
        new("status.txt", "severity", "SEV-2", "Severity: SEV-2"),
        new("status.txt", "incident_id", "INC-042", "INC-042 STATUS PAGE"),
        new("status.txt", "affected_customers", "7", "Impact: 7 customers could not submit construction inspection forms."),
        new("customer-email.txt", "cause", "new billing system", "the new billing system")
    ];

    /// <summary>Records compare collection members by reference, so artifacts are compared as bytes.</summary>
    [Fact]
    public void LedgerRoundTripsThroughJson()
    {
        var json = WorkshopJson.Serialize(LedgerBuilder.Build(Sample));
        var restored = WorkshopJson.Deserialize<ClaimLedger>(json);

        Assert.Equal(json, WorkshopJson.Serialize(restored));
        Assert.Equal(4, restored.Claims.Count);
        Assert.Equal("INC-042", restored.IncidentId);
    }

    [Fact]
    public void SerializedLedgerIsByteStable()
    {
        Assert.Equal(WorkshopJson.Serialize(LedgerBuilder.Build(Sample)), WorkshopJson.Serialize(LedgerBuilder.Build(Sample)));
    }

    [Fact]
    public void ClaimIdsDoNotDependOnExtractionOrder() =>
        Assert.Equal(
            WorkshopJson.Serialize(LedgerBuilder.Build(Sample)),
            WorkshopJson.Serialize(LedgerBuilder.Build(Sample.Reverse())));

    [Fact]
    public void HeaderComesFromTheModelClaimsNotFromSourceParsing()
    {
        var wrong = LedgerBuilder.Build([new ExtractedClaim("status.txt", "severity", "SEV-4", "Severity: SEV-2")]);

        Assert.Equal("SEV-4", wrong.Severity);
        Assert.Contains(VerificationRules.Severity, TestFixtures.Verify(wrong).RuleIdsWithStatus(VerificationStatus.Fail));
    }

    [Theory]
    [InlineData("Affected Customers", "affected_customers")]
    [InlineData("incident-id", "incident_id")]
    [InlineData("  SEVERITY  ", "severity")]
    public void KindSpellingDriftIsNormalized(string raw, string expected) =>
        Assert.Equal(expected, LedgerBuilder.NormalizeKind(raw));

    [Fact]
    public void SameFactFromTwoSourcesBecomesOneClaimWithTwoCitations()
    {
        var ledger = LedgerBuilder.Build(
        [
            new ExtractedClaim("status.txt", "severity", "SEV-2", "Severity: SEV-2"),
            new ExtractedClaim("runbook.md", "severity", "SEV-2", "Escalate SEV-2 incidents to the on-call lead within 15 minutes.")
        ]);

        var claim = Assert.Single(ledger.Claims);
        Assert.Equal(2, claim.Evidence.Count);
    }

    [Fact]
    public void EmptyValuesAreDropped() =>
        Assert.Empty(LedgerBuilder.Build([new ExtractedClaim("status.txt", "severity", "   ", "Severity: SEV-2")]).Claims);

    [Fact]
    public void MissingClaimsProduceAnEmptyHeaderRatherThanAGuess()
    {
        var ledger = LedgerBuilder.Build([]);

        Assert.Empty(ledger.IncidentId);
        Assert.Equal(0, ledger.AffectedCustomers);
    }
}
