using System.Text.Json;
using Microsoft.Agents.AI;

internal enum CrashGate { Supported, NoEvidence, UnsupportedSelection, LowConfidence }

/// <summary>Where code decides. The model proposes; nothing here asks it to confirm.</summary>
internal static class Gates
{
    /// <summary>Malformed output is a gate decision, not an exception: null flows on to Validate.</summary>
    public static T? TryTyped<T>(AgentResponse<T> response) where T : class
    {
        try { return response.Result; }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException) { return null; }
    }

    /// <summary>Every ID must be one this run actually gathered, exactly once.</summary>
    public static CrashGate ValidateSelection(IReadOnlyList<CrashRecord> evidence, CrashSelection? selection, out IReadOnlyList<CrashRecord> selected)
    {
        selected = [];
        if (evidence.Count == 0) return CrashGate.NoEvidence;
        if (selection?.RecordIds is not { Length: > 0 } ids || ids.Length > evidence.Count
            || selection.Confidence is < 0 or > 100 || string.IsNullOrWhiteSpace(selection.Rationale))
            return CrashGate.UnsupportedSelection;

        var allowed = evidence.ToDictionary(r => r.Id, StringComparer.Ordinal);
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length
            || ids.Any(id => string.IsNullOrWhiteSpace(id) || !allowed.ContainsKey(id)))
            return CrashGate.UnsupportedSelection;

        selected = ids.Select(id => allowed[id]).ToList();
        return selection.Confidence < 60 ? CrashGate.LowConfidence : CrashGate.Supported;
    }

}
