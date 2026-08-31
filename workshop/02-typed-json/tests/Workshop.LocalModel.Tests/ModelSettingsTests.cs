using Workshop.App;
using Xunit;

namespace Workshop.LocalModel.Tests;

/// <summary>
/// The provider seam, checked without a model and without authenticating to anything. Swapping
/// lanes is the documented recovery path, so which lane a run is on must be a fact about the
/// configuration rather than a label a human typed.
/// </summary>
public class ModelSettingsTests
{
    private static ModelSettings With(string? endpoint = null, string? key = null, string? model = null, string? budget = null) =>
        ModelSettings.From(endpoint, key, model, budget);

    [Fact]
    public void DefaultsAreTheBlessedLocalLane()
    {
        var settings = With();

        Assert.Equal(ModelSettings.DefaultEndpoint, settings.Endpoint);
        Assert.Equal(ModelSettings.DefaultModel, settings.Model);
        Assert.Equal(ModelLane.Local, settings.Lane);
        Assert.Equal("LOCAL", settings.LaneLabel);
    }

    [Theory]
    [InlineData("http://localhost:11434/v1", "LOCAL")]
    [InlineData("http://127.0.0.1:11434/v1", "LOCAL")]
    [InlineData("http://[::1]:1234/v1", "LOCAL")]
    [InlineData("http://LOCALHOST:1234/v1", "LOCAL")]
    [InlineData("https://example-resource.openai.azure.com/openai/v1", "HOSTED")]
    [InlineData("https://api.openai.com/v1", "HOSTED")]
    [InlineData("http://192.168.1.40:11434/v1", "HOSTED")]
    public void LaneIsDerivedFromTheEndpointNotDeclared(string endpoint, string expected) =>
        Assert.Equal(expected, With(endpoint: endpoint).LaneLabel);

    [Theory]
    [InlineData("http://localhost:11434/v1", "http://localhost:11434")]
    [InlineData("http://localhost:11434/v1/", "http://localhost:11434")]
    [InlineData("http://localhost:11434", "http://localhost:11434")]
    public void RuntimeApiRootSitsBesideTheOpenAiCompatiblePath(string endpoint, string expected) =>
        Assert.Equal(expected, With(endpoint: endpoint).RuntimeApiRoot);

    [Theory]
    [InlineData(null, ModelSettings.DefaultBudgetSeconds)]
    [InlineData("", ModelSettings.DefaultBudgetSeconds)]
    [InlineData("not-a-number", ModelSettings.DefaultBudgetSeconds)]
    [InlineData("45", 45)]
    [InlineData("0", 5)]
    [InlineData("100000", 600)]
    public void RequestBudgetIsClampedSoTheCeilingCannotBeTypoedAway(string? configured, int expectedSeconds) =>
        Assert.Equal(expectedSeconds, (int)With(budget: configured).RequestBudget.TotalSeconds);

    [Fact]
    public void BlankConfigurationFallsBackRatherThanProducingAnEmptyEndpoint()
    {
        var settings = With(endpoint: "   ", key: "  ", model: "\t");

        Assert.Equal(ModelSettings.DefaultEndpoint, settings.Endpoint);
        Assert.NotEmpty(settings.ApiKey);
        Assert.Equal(ModelSettings.DefaultModel, settings.Model);
    }

    /// <summary>The recovery lane is an endpoint swap and a key. No code path, and no new dependency, is required for it.</summary>
    [Fact]
    public void HostedRecoveryLaneNeedsOnlyConfiguration()
    {
        var hosted = With(
            endpoint: "https://example-resource.openai.azure.com/openai/v1",
            key: "placeholder-not-a-real-key",
            model: "gpt-5.4-mini");

        Assert.Equal(ModelLane.Hosted, hosted.Lane);
        Assert.Equal("gpt-5.4-mini", hosted.Model);
        Assert.Equal(ModelSettings.DefaultBudgetSeconds, (int)hosted.RequestBudget.TotalSeconds);
    }
}
