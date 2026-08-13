using System.Text;
using System.Text.RegularExpressions;

namespace Workshop.Core;

public static partial class TextNormalization
{
    /// <summary>
    /// NFC, collapse every whitespace run to one space, trim. Applied to both sides before
    /// substring comparison so a quote that differs only in line wrapping still matches.
    /// </summary>
    public static string Normalize(string? text) =>
        text is null ? string.Empty : Whitespace().Replace(text.Normalize(NormalizationForm.FormC), " ").Trim();

    /// <summary>
    /// Scoped to one physical line of the source. Normalizing the whole file first would let a
    /// quote splice two unrelated lines together and still "occur" in it.
    /// </summary>
    public static bool ContainsQuote(string sourceText, string quote)
    {
        var needle = Normalize(quote);
        return needle.Length != 0 && Lines(sourceText).Any(line => line.Contains(needle, StringComparison.Ordinal));
    }

    public static IEnumerable<string> Lines(string? sourceText) =>
        (sourceText ?? string.Empty).Split('\n').Select(Normalize).Where(line => line.Length != 0);

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
