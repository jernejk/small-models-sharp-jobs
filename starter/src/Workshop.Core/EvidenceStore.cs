namespace Workshop.Core;

public sealed class EvidenceAccessException(string message) : Exception(message);

/// <summary>
/// The only way anything in this application reaches the evidence pack.
/// The whitelist is the guard; the resolved-path check is defence in depth.
/// </summary>
public sealed class EvidenceStore(string evidenceRoot)
{
    public static readonly IReadOnlyList<string> Whitelist =
    [
        "status.txt", "customer-email.txt", "events.csv", "runbook.md"
    ];

    private readonly string _root = Path.GetFullPath(evidenceRoot);

    public string Root => _root;

    public static bool IsWhitelisted(string? sourceId) =>
        sourceId is not null && Whitelist.Contains(sourceId, StringComparer.Ordinal);

    public bool TryRead(string? sourceId, out string content, out string error)
    {
        // TODO 1: Only whitelisted evidence IDs may be read. Reject everything else.
        throw new NotImplementedException("TODO 1: only whitelisted evidence IDs may be read");
    }

    public string Read(string sourceId) =>
        TryRead(sourceId, out var content, out var error) ? content : throw new EvidenceAccessException(error);

    /// <summary>Model-facing shape: errors come back as text so the agent can recover instead of crashing.</summary>
    public string ReadForAgent(string sourceId) =>
        TryRead(sourceId, out var content, out var error) ? content : $"ERROR: {error}";
}
