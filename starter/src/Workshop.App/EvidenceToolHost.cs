using System.ComponentModel;
using Workshop.Core;

namespace Workshop.App;

/// <summary>
/// Wraps <see cref="EvidenceStore"/> as the single tool the agent is allowed to call, and records
/// what it asked for. The recorded calls are what the tool-contract gate checks.
/// </summary>
internal sealed class EvidenceToolHost(EvidenceStore store)
{
    public List<string> Calls { get; } = [];

    /// <summary>Text the tool actually returned, keyed by source id. Extraction reads only from here.</summary>
    public Dictionary<string, string> Fetched { get; } = new(StringComparer.Ordinal);

    public void Reset()
    {
        Calls.Clear();
        Fetched.Clear();
    }

    [Description("Read one whitelisted evidence file from the incident pack. Returns its full text.")]
    public string ReadEvidence(
        [Description("Evidence file name, for example status.txt")] string sourceId)
    {
        Calls.Add(sourceId);

        if (!store.TryRead(sourceId, out var content, out var error))
        {
            return $"ERROR: {error}";
        }

        Fetched[sourceId] = content;
        return content;
    }
}
