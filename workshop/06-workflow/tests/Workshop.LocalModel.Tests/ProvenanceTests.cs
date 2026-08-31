using System.Text.Json;
using Workshop.App;
using Xunit;

namespace Workshop.LocalModel.Tests;

/// <summary>
/// Provenance is read from whatever runtime happens to be on the endpoint. A runtime that is not
/// Ollama must degrade to "unavailable" rather than take the gate run down with it.
/// </summary>
public class ProvenanceTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    [Fact]
    public void OllamaVersionIsRead() =>
        Assert.Equal("0.32.9", Provenance.RuntimeVersionFrom(Json("""{"version":"0.32.9"}""")));

    [Fact]
    public void LmStudioErrorBodyDegradesInsteadOfThrowing() =>
        Assert.Equal("unavailable", Provenance.RuntimeVersionFrom(
            Json("""{"error":"Unexpected endpoint or method. (GET /api/version)"}""")));

    [Fact]
    public void EmptyBodyDegradesInsteadOfThrowing() =>
        Assert.Equal("unavailable", Provenance.RuntimeVersionFrom(Json("{}")));
}
