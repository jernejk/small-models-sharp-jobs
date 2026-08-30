using Workshop.Core;
using Xunit;

namespace Workshop.Core.Tests;

public class VictoriaRoadCrashCorpusTests
{
    private static readonly IReadOnlyList<IncidentRecord> Records = IncidentDataset.Load(
        Path.Combine(AppContext.BaseDirectory, "workshop-data", "victoria-road-crash-sample.json"));

    [Fact]
    public void CorpusIsACompactAttributedHistoricalSubset()
    {
        Assert.InRange(Records.Count, 20, 50);
        Assert.All(Records, record =>
        {
            Assert.StartsWith("T20", record.Id);
            Assert.StartsWith("Victoria Road Crash Data: ", record.SourceReference);
            Assert.InRange(record.Date.Year, 2013, 2025);
            Assert.NotEqual("Fatal accident", record.Severity);
            Assert.DoesNotContain("pedestrian", record.Summary, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void CorpusSupportsTheDesignedGatherOutcomes()
    {
        var supported = IncidentDataset.Gather(Records,
            new IncidentQuery(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31), "rear-end"));
        var ambiguous = IncidentDataset.Gather(Records, new IncidentQuery(null, null, "collision", 8));
        var noResult = IncidentDataset.Gather(Records,
            new IncidentQuery(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31), "cyclist"));

        Assert.Equal(["T20240010964"], supported.Records.Select(record => record.Id));
        Assert.Equal(8, ambiguous.Records.Count);
        Assert.True(noResult.IsEmpty);
    }
}
