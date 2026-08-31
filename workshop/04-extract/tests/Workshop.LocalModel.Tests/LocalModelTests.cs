using System.Diagnostics;
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
/// The fast confidence check against a real model: the two setup checkpoints, the supported crash
/// path, the branch that must never reach the model, and the warm latency budget.
/// </summary>
public class LocalModelTests
{
    private const string Question = "What patterns should an insurance and safety analyst review in the selected crash records?";

    private static ModelSettings Settings() => ModelSettings.FromEnvironment();

    private static CrashPipeline Pipeline() => new(Settings());

    private static EvidencePack Gather(string term) =>
        IncidentDataset.Gather(
            IncidentDataset.Load(WorkshopPaths.Resolve(null, "workshop/data/victoria-road-crash-sample.json")),
            new IncidentQuery(null, null, term));

    [LocalModelFact]
    public async Task SmokeReturnsTheExactToken()
    {
        var check = await Pipeline().HelloAsync();

        Assert.True(check.TokenPresent, $"reply was '{check.Reply}'");
    }

    [LocalModelFact]
    public async Task TypedCallParsesIntoTheContract()
    {
        var check = await Pipeline().TypedAsync();

        Assert.True(check.IsValid, $"raw was '{check.Raw}'");
        Assert.Equal("mute", check.Value!.Action, ignoreCase: true);
        Assert.Equal("microphone", check.Value.Target, ignoreCase: true);
    }

    [LocalModelFact]
    public async Task SupportedTermSelectsRecordsAndProducesAFinding()
    {
        var evidence = Gather("intersection");
        Assert.False(evidence.IsEmpty);

        var run = await Pipeline().RunAsync(Question, evidence);

        Assert.Equal(CrashGate.Supported, run.Gate);
        Assert.NotEmpty(run.Selection!.RecordIds);
        Assert.All(run.Selection.RecordIds, id => Assert.Contains(evidence.Records, record => record.Id == id));
        Assert.False(string.IsNullOrWhiteSpace(run.Analysis!.Finding));
    }

    [LocalModelFact]
    public async Task NoResultTermStopsBeforeExtractAndAnalyse()
    {
        var evidence = Gather("no-such-term-in-the-approved-sample");
        Assert.True(evidence.IsEmpty);

        var started = Stopwatch.GetTimestamp();
        var run = await Pipeline().RunAsync(Question, evidence);

        Assert.Equal(CrashGate.NoEvidence, run.Gate);
        Assert.Null(run.Selection);
        Assert.Null(run.Analysis);
        Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(1), "the no-evidence branch reached the model");
    }

    [LocalModelFact]
    public async Task WarmFullPathStaysWithinTheRequestBudget()
    {
        var evidence = Gather("intersection");
        var pipeline = Pipeline();
        await pipeline.RunAsync(Question, evidence);

        var started = Stopwatch.GetTimestamp();
        await pipeline.RunAsync(Question, evidence);
        var warm = Stopwatch.GetElapsedTime(started);

        Assert.True(warm <= Settings().RequestBudget,
            $"warm full path took {warm.TotalSeconds:F1}s against a {Settings().RequestBudget.TotalSeconds:F0}s budget");
    }
}
