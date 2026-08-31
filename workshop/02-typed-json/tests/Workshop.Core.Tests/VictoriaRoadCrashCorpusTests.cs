using Workshop.Core;
using Xunit;

namespace Workshop.Core.Tests;

public class VictoriaRoadCrashCorpusTests
{
    private static readonly IReadOnlyList<IncidentRecord> Records = IncidentDataset.Load(
        Path.Combine(AppContext.BaseDirectory, "workshop-data", "victoria-road-crash-sample.json"));

    [Fact]
    public void CorpusIsADeidentifiedAttributedHistoricalSubset()
    {
        Assert.Equal(1000, Records.Count);
        Assert.All(Records, record =>
        {
            Assert.StartsWith("T20", record.Id);
            Assert.StartsWith("Victoria Road Crash Data: ", record.SourceReference);
            Assert.InRange(record.Date.Year, 2012, 2025);
            Assert.NotEqual("Fatal accident", record.Severity);
            Assert.DoesNotContain("pedestrian", record.Summary, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void CorpusSupportsTheDesignedGatherOutcomes()
    {
        var supported = IncidentDataset.Gather(Records,
            new IncidentQuery(null, null, "intersection", 8));
        var capped = IncidentDataset.Gather(Records, new IncidentQuery(null, null, null, 8));
        var noResult = IncidentDataset.Gather(Records, new IncidentQuery(null, null, "definitely-not-present"));

        Assert.Equal(8, supported.Records.Count);
        Assert.All(supported.Records, record => Assert.Contains("intersection", $"{record.Title} {record.Summary}", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(8, capped.Records.Count);
        Assert.True(noResult.IsEmpty);
    }
}
