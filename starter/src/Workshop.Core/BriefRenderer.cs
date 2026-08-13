using System.Text;

namespace Workshop.Core;

/// <summary>
/// The authoritative brief. Deterministic code writes every byte of it; the model writes none.
/// Output depends only on the ledger, the report and the parsed facts, so identical inputs
/// always produce an identical file.
/// </summary>
public static class BriefRenderer
{
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:sszzz";

    public static string Render(ClaimLedger ledger, VerificationReport report, SourceFacts facts)
    {
        var failed = report.FailedClaimIds;
        var unverified = report.UnverifiedClaimIds;
        var brief = new StringBuilder();

        var headerId = failed.Contains(VerificationRules.LedgerScope) ? "unverified incident" : ledger.IncidentId;
        Line(brief, $"# Incident brief: {headerId}");
        Line(brief);
        Line(brief, "Rendered by deterministic code from verified claims only. The model did not write this file.");
        Line(brief);

        Line(brief, "## Verified facts");
        Line(brief);
        var verified = ledger.Claims
            .Where(c => !failed.Contains(c.Id) && !unverified.Contains(c.Id))
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .ToList();

        if (verified.Count == 0)
        {
            Line(brief, "_No claim passed verification._");
        }
        else
        {
            Line(brief, "| Claim | Kind | Value | Evidence |");
            Line(brief, "| --- | --- | --- | --- |");
            foreach (var claim in verified)
            {
                Line(brief, $"| {claim.Id} | {claim.Kind} | {Cell(claim.Value)} | {Citations(claim)} |");
            }
        }
        Line(brief);

        Line(brief, "## Timeline");
        Line(brief);
        Line(brief, "Parsed from `events.csv` by code, not by the model.");
        Line(brief);
        Line(brief, "| Time | Event |");
        Line(brief, "| --- | --- |");
        foreach (var entry in facts.Timeline)
        {
            Line(brief, $"| {entry.Timestamp.ToString(TimestampFormat)} | {Cell(entry.Description)} |");
        }
        Line(brief);
        Line(brief, $"Duration from source parsing: {(int)facts.Duration.TotalMinutes} minutes "
            + $"({facts.StartedAt.ToString(TimestampFormat)} to {facts.ResolvedAt.ToString(TimestampFormat)}).");
        Line(brief);

        Line(brief, "## Shown but not verified");
        Line(brief);
        var unverifiedClaims = ledger.Claims
            .Where(c => unverified.Contains(c.Id) && !failed.Contains(c.Id))
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .ToList();

        if (unverifiedClaims.Count == 0)
        {
            Line(brief, "_None._");
        }
        else
        {
            Line(brief, "Deterministic checks cannot confirm these. They are reported, not asserted.");
            Line(brief);
            foreach (var claim in unverifiedClaims)
            {
                Line(brief, $"- **{claim.Kind}**: {Cell(claim.Value)} — {Citations(claim)}");
                foreach (var reason in report.Results.Where(r => r.ClaimId == claim.Id && r.Status == VerificationStatus.Unverified))
                {
                    Line(brief, $"  - {reason.RuleId}: {reason.Detail}");
                }
            }
        }
        Line(brief);

        Line(brief, "## Excluded by verification");
        Line(brief);
        var failures = report.Results.Where(r => r.Status == VerificationStatus.Fail).ToList();
        if (failures.Count == 0)
        {
            Line(brief, "_No claim failed verification._");
        }
        else
        {
            Line(brief, $"{failed.Count} item(s) failed verification and are excluded from the sections above.");
            Line(brief);
            foreach (var failure in failures.OrderBy(r => r.ClaimId, StringComparer.Ordinal).ThenBy(r => r.RuleId, StringComparer.Ordinal))
            {
                Line(brief, $"- {failure.ClaimId} — {failure.RuleId}: {Cell(failure.Detail)}");
            }
        }
        Line(brief);

        Line(brief, "## Verification summary");
        Line(brief);
        Line(brief, $"- passed: {report.Passed}");
        Line(brief, $"- failed: {report.Failed}");
        Line(brief, $"- unverified: {report.Unverified}");
        Line(brief);
        Line(brief, "Full detail is in `verification.json`.");

        return brief.ToString();
    }

    /// <summary>One `## ` section of a rendered brief, so callers can assert on where a claim landed.</summary>
    public static string Section(string brief, string heading)
    {
        var start = brief.IndexOf(heading, StringComparison.Ordinal);
        if (start < 0) return string.Empty;
        var next = brief.IndexOf("\n## ", start + heading.Length, StringComparison.Ordinal);
        return next < 0 ? brief[start..] : brief[start..next];
    }

    private static string Citations(Claim claim) =>
        claim.Evidence.Count == 0
            ? "_no evidence cited_"
            : string.Join("; ", claim.Evidence.Select(e => $"`{e.SourceId}`: \"{Cell(e.ExactQuote)}\""));

    private static string Cell(string? text) =>
        TextNormalization.Normalize(text).Replace("|", "\\|", StringComparison.Ordinal);

    private static void Line(StringBuilder builder, string text = "") => builder.Append(text).Append('\n');
}
