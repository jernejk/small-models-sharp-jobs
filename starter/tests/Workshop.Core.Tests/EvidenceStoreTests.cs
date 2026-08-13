using Workshop.Core;
using Xunit;

namespace Workshop.Core.Tests;

public class EvidenceStoreTests
{
    [Theory]
    [InlineData("../evidence-pack/status.txt")]
    [InlineData("../../etc/passwd")]
    [InlineData("/etc/passwd")]
    [InlineData("status.txt/../runbook.md")]
    [InlineData("./status.txt")]
    [InlineData("evidence-pack/status.txt")]
    public void RejectsPathTraversal(string sourceId)
    {
        Assert.False(TestFixtures.Store().TryRead(sourceId, out _, out var error));
        Assert.Contains("unknown evidence id", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("secrets.txt")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("STATUS.TXT")]
    public void RejectsUnknownIds(string sourceId) =>
        Assert.False(TestFixtures.Store().TryRead(sourceId, out _, out _));

    /// <summary>The answer key sits in the same directory. Existing on disk must not be enough.</summary>
    [Fact]
    public void RejectsRealFileThatIsNotWhitelisted()
    {
        Assert.True(File.Exists(Path.Combine(TestFixtures.EvidenceDir, "expected-facts.json")));
        Assert.False(TestFixtures.Store().TryRead("expected-facts.json", out _, out var error));
        Assert.Contains("unknown evidence id", error, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadsEveryWhitelistedSource()
    {
        var store = TestFixtures.Store();
        foreach (var sourceId in EvidenceStore.Whitelist)
        {
            Assert.True(store.TryRead(sourceId, out var content, out _));
            Assert.NotEmpty(content);
        }
    }

    [Fact]
    public void ReadThrowsOnRejection() =>
        Assert.Throws<EvidenceAccessException>(() => TestFixtures.Store().Read("../secrets"));

    [Fact]
    public void AgentFacingReadReturnsErrorTextRatherThanThrowing() =>
        Assert.StartsWith("ERROR:", TestFixtures.Store().ReadForAgent("../secrets"), StringComparison.Ordinal);
}
