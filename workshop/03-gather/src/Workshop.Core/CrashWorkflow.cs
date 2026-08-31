namespace Workshop.Core;

/// <summary>Typed output from the focused Extract job. IDs must come from the supplied evidence pack.</summary>
public sealed record CrashSelection(string[] RecordIds, string Rationale, int Confidence);

/// <summary>Typed output from the focused Analyse job. It never receives the full corpus.</summary>
public sealed record CrashAnalysis(string Finding, string[] Actions, string[] Questions, int Confidence);

public enum CrashGate { Supported, NoEvidence, UnsupportedSelection, LowConfidence, UnsupportedAnalysis }

public static class CrashWorkflow
{
    public static CrashGate ValidateSelection(EvidencePack pack, CrashSelection? selection, out IReadOnlyList<IncidentRecord> selected)
    {
        selected = [];
        if (pack.IsEmpty) return CrashGate.NoEvidence;
        if (selection is null || selection.RecordIds is null || selection.RecordIds.Length is 0 || selection.RecordIds.Length > pack.Records.Count
            || selection.Confidence is < 0 or > 100)
            return CrashGate.UnsupportedSelection;

        var allowed = pack.Records.ToDictionary(r => r.Id, StringComparer.Ordinal);
        if (selection.RecordIds.Distinct(StringComparer.Ordinal).Count() != selection.RecordIds.Length
            || selection.RecordIds.Any(id => string.IsNullOrWhiteSpace(id) || !allowed.ContainsKey(id))
            || string.IsNullOrWhiteSpace(selection.Rationale))
            return CrashGate.UnsupportedSelection;

        selected = selection.RecordIds.Select(id => allowed[id]).ToList();
        return selection.Confidence < 60 ? CrashGate.LowConfidence : CrashGate.Supported;
    }

    public static CrashGate ValidateAnalysis(IReadOnlyList<IncidentRecord> selected, CrashAnalysis? analysis)
    {
        if (selected.Count == 0 || analysis is null || analysis.Confidence is < 0 or > 100
            || string.IsNullOrWhiteSpace(analysis.Finding) || analysis.Actions is null || analysis.Questions is null)
            return CrashGate.UnsupportedAnalysis;
        return analysis.Confidence < 60 ? CrashGate.LowConfidence : CrashGate.Supported;
    }
}
