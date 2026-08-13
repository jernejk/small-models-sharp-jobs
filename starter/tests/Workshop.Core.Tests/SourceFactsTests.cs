using System.Text.Json;
using Workshop.Core;
using Xunit;

namespace Workshop.Core.Tests;

public class SourceFactsTests
{
    /// <summary>
    /// expected-facts.json is the hidden answer key. If the deterministic parser and the answer key
    /// ever disagree, every downstream verification result is worthless.
    /// </summary>
    [Fact]
    public void ParserAgreesWithHiddenAnswerKey()
    {
        var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestFixtures.EvidenceDir, "expected-facts.json"))).RootElement;
        var facts = TestFixtures.Facts();

        Assert.Equal(expected.GetProperty("incidentId").GetString(), facts.IncidentId);
        Assert.Equal(expected.GetProperty("severity").GetString(), facts.Severity);
        Assert.Equal(expected.GetProperty("affectedCustomers").GetInt32(), facts.AffectedCustomers);
        Assert.Equal(SourceFactsParser.ParseTimestamp(expected.GetProperty("startedAt").GetString()!), facts.StartedAt);
        Assert.Equal(SourceFactsParser.ParseTimestamp(expected.GetProperty("resolvedAt").GetString()!), facts.ResolvedAt);
        Assert.Equal(expected.GetProperty("durationMinutes").GetInt32(), (int)facts.Duration.TotalMinutes);
        Assert.Equal(expected.GetProperty("timelineEventCount").GetInt32(), facts.Timeline.Count);
        Assert.Equal(expected.GetProperty("firstTimelineEvent").GetString(), facts.Timeline[0].Description);
        Assert.Equal(expected.GetProperty("lastTimelineEvent").GetString(), facts.Timeline[^1].Description);
    }

    [Fact]
    public void TimelineIsParsedInOrder()
    {
        var timeline = TestFixtures.Facts().Timeline;
        Assert.Equal(timeline.OrderBy(e => e.Timestamp), timeline);
    }

    [Fact]
    public void KnownTimestampsCoverBoundariesAndTimeline()
    {
        var facts = TestFixtures.Facts();
        Assert.Contains(facts.StartedAt, facts.KnownTimestamps);
        Assert.Contains(facts.ResolvedAt, facts.KnownTimestamps);
        Assert.All(facts.Timeline, e => Assert.Contains(e.Timestamp, facts.KnownTimestamps));
    }
}
