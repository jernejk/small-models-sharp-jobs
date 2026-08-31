using Workshop.Core;
using Xunit;

namespace Workshop.Core.Tests;

public class CrashWorkflowTests
{
    private static readonly IncidentRecord[] Pack =
    [
        new("VIC-1", new DateOnly(2024, 1, 1), "Rear end", "rear end collision", "other injury", "source:1"),
        new("VIC-2", new DateOnly(2024, 1, 2), "Intersection", "cross traffic", "serious injury", "source:2")
    ];

    [Fact]
    public void NoEvidenceStopsBeforeAnyModelResult() =>
        Assert.Equal(CrashGate.NoEvidence, CrashWorkflow.ValidateSelection(new EvidencePack([]), null, out _));

    [Fact]
    public void UnknownOrDuplicateSourceIdsAreRejected()
    {
        var evidence = new EvidencePack(Pack);
        Assert.Equal(CrashGate.UnsupportedSelection, CrashWorkflow.ValidateSelection(evidence, new CrashSelection(["VIC-1", "NOPE"], "x", 90), out _));
        Assert.Equal(CrashGate.UnsupportedSelection, CrashWorkflow.ValidateSelection(evidence, new CrashSelection(["VIC-1", "VIC-1"], "x", 90), out _));
    }

    [Fact]
    public void LowConfidenceNeverBecomesSupported()
    {
        var gate = CrashWorkflow.ValidateSelection(new EvidencePack(Pack), new CrashSelection(["VIC-1"], "x", 40), out var selected);
        Assert.Equal(CrashGate.LowConfidence, gate);
        Assert.Single(selected);
    }

    [Fact]
    public void ValidSelectionAndAnalysisAreSupported()
    {
        var selectionGate = CrashWorkflow.ValidateSelection(new EvidencePack(Pack), new CrashSelection(["VIC-1"], "matches question", 90), out var selected);
        var analysisGate = CrashWorkflow.ValidateAnalysis(selected, new CrashAnalysis("A grounded pattern", ["review"], ["what else?"], 80));
        Assert.Equal(CrashGate.Supported, selectionGate);
        Assert.Equal(CrashGate.Supported, analysisGate);
    }
}
