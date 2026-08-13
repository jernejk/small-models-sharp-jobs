using Workshop.App;
using Workshop.Core;
using Xunit;

namespace Workshop.LocalModel.Tests;

/// <summary>Marks a test that needs a real model endpoint, so a default `dotnet test` stays offline and fast.</summary>
public sealed class LocalModelFactAttribute : FactAttribute
{
    public LocalModelFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("WORKSHOP_LOCAL_MODEL") != "1")
        {
            Skip = "set WORKSHOP_LOCAL_MODEL=1 with the local runtime running";
        }
    }
}

/// <summary>
/// The fast confidence check against a real model. The full gate matrix
/// (L1-L6, seeded defects, latency) lives in `Workshop.App gates`.
/// </summary>
public class LocalModelTests
{
    private static string EvidenceDir
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "evidence-pack");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException("evidence-pack not found above the test binary");
        }
    }

    private static IncidentPipeline Pipeline() =>
        new(ModelSettings.FromEnvironment(), new EvidenceStore(EvidenceDir));

    [LocalModelFact]
    public async Task SmokeReturnsTheExactToken() =>
        Assert.Equal("JACKDAW_OK", await Pipeline().SmokeAsync());

    [LocalModelFact]
    public async Task FullPathProducesAVerifiedLedger()
    {
        var result = await Pipeline().RunAsync();

        Assert.True(result.ToolContractHeld, $"tool calls were [{string.Join(", ", result.ToolCalls)}]");
        Assert.True(result.TypedExtractionValid);
        Assert.False(result.Report.HasFailures,
            $"failed rules: {string.Join(", ", result.Report.RuleIdsWithStatus(VerificationStatus.Fail))}");
        Assert.Equal("INC-042", result.Ledger.IncidentId);
        Assert.Equal(7, result.Ledger.AffectedCustomers);
    }

    [LocalModelFact]
    public async Task WarmFullPathStaysWithinTheLatencyBudget()
    {
        await Pipeline().RunAsync();
        var warm = await Pipeline().RunAsync();

        Assert.True(warm.TotalSeconds <= 30.0, $"warm full path took {warm.TotalSeconds}s");
    }

    [LocalModelFact]
    public async Task SeededDefectsAreCaughtInTheRealPipeline()
    {
        var clean = await Pipeline().RunAsync();
        var facts = SourceFactsParser.Parse(new EvidenceStore(EvidenceDir));

        foreach (var defect in new[] { SeededDefect.PhantomSource, SeededDefect.AlteredNumber, SeededDefect.AlteredTimestamp })
        {
            var report = Verifier.Verify(DefectInjector.Inject(clean.Ledger, defect), facts, new EvidenceStore(EvidenceDir));
            Assert.Contains(DefectInjector.ExpectedRuleId(defect), report.RuleIdsWithStatus(VerificationStatus.Fail));
        }
    }
}
