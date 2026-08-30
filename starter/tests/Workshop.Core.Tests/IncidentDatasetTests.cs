using Workshop.Core;
using Xunit;

namespace Workshop.Core.Tests;

public class IncidentDatasetTests
{
    private static readonly IncidentRecord[] Records =
    [
        new("INC-100", new DateOnly(2026, 8, 1), "Billing timeout", "Customers saw a timeout during billing.", "high", "synthetic:INC-100"),
        new("INC-101", new DateOnly(2026, 8, 5), "Cache refresh", "A cache refresh completed without customer impact.", "low", "synthetic:INC-101"),
        new("INC-102", new DateOnly(2026, 8, 8), "Billing recovery", "Billing was restored after a routing change.", "medium", "synthetic:INC-102")
    ];

    [Fact]
    public void GatherFiltersByDateAndTerm() =>
        Assert.Equal(["INC-102", "INC-100"], IncidentDataset.Gather(Records,
            new IncidentQuery(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 8), "billing")).Records.Select(x => x.Id));

    [Fact]
    public void GatherReturnsClearEmptyPackWhenNothingMatches() =>
        Assert.True(IncidentDataset.Gather(Records, new IncidentQuery(null, null, "database")).IsEmpty);

    [Fact]
    public void GatherCapsTheEvidencePack() =>
        Assert.Single(IncidentDataset.Gather(Records, new IncidentQuery(null, null, null, 1)).Records);
}
