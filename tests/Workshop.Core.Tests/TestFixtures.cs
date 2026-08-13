using Workshop.Core;

namespace Workshop.Core.Tests;

internal static class TestFixtures
{
    public static string EvidenceDir
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "evidence-pack");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException("evidence-pack not found above the test binary");
        }
    }

    public static EvidenceStore Store() => new(EvidenceDir);

    public static SourceFacts Facts() => SourceFactsParser.Parse(Store());

    /// <summary>Mirrors what the model produces on a clean run, so tests never need a model.</summary>
    public static ClaimLedger CleanLedger() => new(
        IncidentId: "INC-042",
        Severity: "SEV-2",
        AffectedCustomers: 7,
        Claims:
        [
            new Claim("C001", ClaimKinds.IncidentId, "INC-042",
                [new EvidenceReference("status.txt", "INC-042 STATUS PAGE")]),
            new Claim("C002", ClaimKinds.Severity, "SEV-2",
                [new EvidenceReference("status.txt", "Severity: SEV-2")]),
            new Claim("C003", ClaimKinds.AffectedCustomers, "7",
                [new EvidenceReference("status.txt", "Impact: 7 customers could not submit construction inspection forms.")]),
            new Claim("C004", ClaimKinds.Timestamp, "2026-08-13T09:12:00+10:00",
                [new EvidenceReference("status.txt", "Started: 2026-08-13T09:12:00+10:00")]),
            new Claim("C005", ClaimKinds.Cause, "new billing system",
                [new EvidenceReference("customer-email.txt", "the new billing system")])
        ]);

    public static VerificationReport Verify(ClaimLedger ledger) => Verifier.Verify(ledger, Facts(), Store());
}
