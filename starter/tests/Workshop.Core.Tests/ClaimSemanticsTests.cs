using Workshop.Core;
using Xunit;

namespace Workshop.Core.Tests;

public class ClaimSemanticsTests
{
    [Theory]
    [InlineData("Caused   BY  the\nbilling system", "caused by the billing system")]
    [InlineData("R.O.U.T.I.N.G — rule, replaced!", "r o u t i n g rule replaced")]
    [InlineData(null, "")]
    public void CanonicalizationIsCaseWhitespaceAndPunctuationInsensitive(string? input, string expected) =>
        Assert.Equal(expected, ClaimSemantics.Canonicalize(input));

    [Theory]
    [InlineData("routing rule replaced", "routing rule", true)]
    [InlineData("routing rule replaced", "rule replaced", true)]
    [InlineData("routing rule replaced", "routing replaced", false)]
    [InlineData("routing rule replaced", "routing rule replaced twice", false)]
    [InlineData("stale routing rule identified", "rout", false)]
    public void TokenSpanMatchingIsContiguousAndWholeWord(string haystack, string needle, bool expected) =>
        Assert.Equal(expected, ClaimSemantics.ContainsTokenSpan(ClaimSemantics.Tokens(haystack), ClaimSemantics.Tokens(needle)));

    [Theory]
    [InlineData("the outage was caused by the new billing system")]
    [InlineData("Root cause: a stale routing rule")]
    [InlineData("submissions failed because of the deploy")]
    [InlineData("the incident was triggered by a cache miss")]
    [InlineData("the billing change led to the outage")]
    [InlineData("downtime attributable to the routing layer")]
    public void CausalWordingIsDetected(string text)
    {
        Assert.True(ClaimSemantics.ContainsAnyMarker(text, out var marker));
        Assert.NotEmpty(marker);
    }

    /// <summary>The runbook says "a confirmed cause" as policy. Firing on that would make the rule noise.</summary>
    [Theory]
    [InlineData("Do not report customer speculation as a confirmed cause.")]
    [InlineData("stale routing rule identified")]
    [InlineData("routing rule replaced")]
    [InlineData("submissions verified healthy")]
    [InlineData("error rate alert fired")]
    [InlineData("Impact: 7 customers could not submit construction inspection forms.")]
    [InlineData("Routing rules are cached for 10 minutes after replacement.")]
    public void OrdinaryIncidentWordingIsNotTreatedAsCausal(string text) =>
        Assert.False(ClaimSemantics.ContainsAnyMarker(text, out _));

    [Fact]
    public void CausalWordingInAQuoteIsDetectedEvenWhenTheValueLooksInnocent()
    {
        var claim = new Claim("C001", ClaimKinds.Event, "billing system",
            [new EvidenceReference("customer-email.txt", "Our team believes the outage was caused by the new billing system.")]);

        var finding = ClaimSemantics.DetectCause(claim);

        Assert.NotNull(finding);
        Assert.Equal("caused by", finding.Marker);
        Assert.Contains("customer-email.txt", finding.Where, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("routing rule replaced", true)]
    [InlineData("Routing Rule Replaced.", true)]
    [InlineData("stale routing rule", true)]
    [InlineData("engineers restarted the billing service", false)]
    [InlineData("routing rule replaced twice", false)]
    [InlineData("", false)]
    public void EventSupportIsGradedAgainstTheParsedEventLog(string value, bool expected) =>
        Assert.Equal(expected, ClaimSemantics.IsSupportedEvent(value, TestFixtures.Facts()));

    [Fact]
    public void AllowedEventPhrasesComeFromTheEventLogOnly()
    {
        var phrases = TestFixtures.Facts().AllowedEventPhrases;

        Assert.Equal(4, phrases.Count);
        Assert.Contains("error rate alert fired", phrases);
        Assert.Contains("submissions verified healthy", phrases);
    }
}
