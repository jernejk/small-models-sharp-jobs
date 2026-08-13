using System.Text;
using System.Text.RegularExpressions;

namespace Workshop.Core;

/// <summary>Which text carried the causal wording, and the marker that matched.</summary>
public sealed record CausalFinding(string Marker, string Where);

/// <summary>
/// Deterministic meaning checks over model free text. No model, no scoring, no judge: every
/// decision here is a token comparison against a fixed marker list or against phrases parsed
/// out of the evidence pack by code.
/// </summary>
public static partial class ClaimSemantics
{
    /// <summary>
    /// Wording that asserts one thing produced another. Matched as whole-token spans, so
    /// "a confirmed cause" in the runbook does not fire and "caused by" does.
    /// </summary>
    public static readonly IReadOnlyList<string> CausalMarkers =
    [
        "caused by", "was caused", "were caused", "root cause", "the cause of", "due to",
        "because", "because of", "resulted from", "resulting from", "as a result of",
        "triggered by", "stems from", "stemmed from", "owing to", "attributed to",
        "attributable to", "blamed on", "brought about by", "responsible for",
        "reason for", "reason was", "led to", "leading to"
    ];

    /// <summary>NFC, lowercase, punctuation to space, whitespace collapsed. Comparison unit is a whole token.</summary>
    public static string Canonicalize(string? text) =>
        text is null
            ? string.Empty
            : Collapse().Replace(NonWord().Replace(text.Normalize(NormalizationForm.FormC).ToLowerInvariant(), " "), " ").Trim();

    public static IReadOnlyList<string> Tokens(string? text) =>
        Canonicalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>True when <paramref name="needle"/> occurs in <paramref name="haystack"/> as a contiguous run of whole tokens.</summary>
    public static bool ContainsTokenSpan(IReadOnlyList<string> haystack, IReadOnlyList<string> needle)
    {
        if (needle.Count == 0 || needle.Count > haystack.Count) return false;

        for (var start = 0; start + needle.Count <= haystack.Count; start++)
        {
            var matched = true;
            for (var offset = 0; offset < needle.Count && matched; offset++)
            {
                matched = string.Equals(haystack[start + offset], needle[offset], StringComparison.Ordinal);
            }
            if (matched) return true;
        }
        return false;
    }

    public static bool ContainsAnyMarker(string? text, out string marker)
    {
        var tokens = Tokens(text);
        foreach (var candidate in CausalMarkers)
        {
            if (ContainsTokenSpan(tokens, Tokens(candidate)))
            {
                marker = candidate;
                return true;
            }
        }
        marker = string.Empty;
        return false;
    }

    /// <summary>
    /// Causal detection that ignores the declared kind. A claim asserting causation is a cause
    /// claim whatever the model labelled it, which is what stops a mislabel from evading R9.
    /// </summary>
    public static CausalFinding? DetectCause(Claim claim)
    {
        if (ContainsAnyMarker(claim.Value, out var inValue))
        {
            return new CausalFinding(inValue, "value");
        }

        foreach (var reference in claim.Evidence)
        {
            if (ContainsAnyMarker(reference.ExactQuote, out var inQuote))
            {
                return new CausalFinding(inQuote, $"quote cited from {reference.SourceId}");
            }
        }

        return null;
    }

    /// <summary>
    /// An event is supported only when its text is a whole-token span of one of the event
    /// descriptions code parsed from events.csv. Free text the log does not contain is not
    /// refuted, it is simply unsupported, so the caller reports it rather than asserting it.
    /// </summary>
    public static bool IsSupportedEvent(string? value, SourceFacts facts)
    {
        var tokens = Tokens(value);
        return tokens.Count > 0 && facts.AllowedEventPhrases.Any(phrase => ContainsTokenSpan(Tokens(phrase), tokens));
    }

    [GeneratedRegex(@"[^\p{L}\p{Nd}]+")]
    private static partial Regex NonWord();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Collapse();
}
