using System.Globalization;
using Workshop.App;
using Workshop.Core;

var command = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? "run";
var evidenceDir = WorkshopPaths.Resolve(Option("--evidence"), "evidence-pack");
var repeat = int.TryParse(Option("--repeat"), CultureInfo.InvariantCulture, out var parsed) ? Math.Clamp(parsed, 1, 20) : 5;
var readyBudget = int.TryParse(Option("--budget"), CultureInfo.InvariantCulture, out var budget) ? Math.Clamp(budget, 10, 600) : 90;
var defect = ParseDefect(Option("--inject-defect"));
var outputDir = WorkshopPaths.Resolve(Option("--out"), DefaultOutputFor(defect));

var settings = ModelSettings.FromEnvironment();
var store = new EvidenceStore(evidenceDir);
var artifactNames = new[] { "claim-ledger.json", "verification.json", "incident-brief.md" };
Directory.CreateDirectory(outputDir);

try
{
    return command switch
    {
        "run" => await RunAsync(),
        "smoke" => await SmokeAsync(),
        "ready" => await ReadyAsync(),
        "gates" => await GatesAsync(),
        "verify-only" => VerifyOnly(),
        _ => Usage()
    };
}
catch (ModelBudgetExceededException ex)
{
    Console.Error.WriteLine($"pipeline error: {ex.Message}");
    return 3;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"pipeline error: {ex.GetType().Name}: {ex.Message}");
    return 3;
}

async Task<int> RunAsync()
{
    Console.WriteLine($"{settings.LaneLabel} run | model={settings.Model} | endpoint={settings.Endpoint} | evidence={evidenceDir}");
    if (defect != SeededDefect.None) Console.WriteLine($"seeded defect: {defect} (expected {Describe(defect)})");

    var pipeline = new IncidentPipeline(settings, store);
    var result = await pipeline.RunAsync(defect);
    WriteArtifacts(result.Ledger, result.Report, result.Brief);

    Console.WriteLine($"tool calls    : [{string.Join(", ", result.ToolCalls)}] contract={(result.ToolContractHeld ? "held" : "BROKEN")}");
    Console.WriteLine($"claims        : {result.Ledger.Claims.Count}");
    Console.WriteLine($"verification  : {result.Report.Passed} passed, {result.Report.Failed} failed, {result.Report.Unverified} unverified");
    Console.WriteLine($"timing        : gather {result.GatherSeconds}s + extract {result.ExtractSeconds}s = {result.TotalSeconds}s");
    Console.WriteLine($"artifacts     : {outputDir}");

    // A broken tool contract means the agent did not fetch what it was told to. Reporting that as a
    // clean run is the failure mode this workshop exists to argue against.
    if (!result.ToolContractHeld)
    {
        Console.Error.WriteLine($"TOOL CONTRACT BROKEN: expected [{string.Join(", ", IncidentPipeline.ProseSources)}], got [{string.Join(", ", result.ToolCalls)}]");
        return 5;
    }

    if (result.Report.HasFailures)
    {
        Console.Error.WriteLine($"FAILED rules: {string.Join(", ", result.Report.RuleIdsWithStatus(VerificationStatus.Fail))}");
        return 2;
    }
    return 0;
}

async Task<int> SmokeAsync()
{
    var text = await new IncidentPipeline(settings, store).SmokeAsync();
    var passed = text == "JACKDAW_OK";
    Console.WriteLine($"smoke: {(passed ? "PASS" : "FAIL")} [{text}]");
    return passed ? 0 : 4;
}

/// <summary>
/// What an attendee needs to know before travelling. A smoke token proves the runtime answers;
/// it does not prove the machine can produce the three artifacts the workshop is built around.
/// </summary>
async Task<int> ReadyAsync()
{
    Console.WriteLine($"READY CHECK | {settings.LaneLabel} | model={settings.Model} | endpoint={settings.Endpoint} | budget={readyBudget}s");

    var result = await new IncidentPipeline(settings, store).RunAsync();
    WriteArtifacts(result.Ledger, result.Report, result.Brief);

    var checks = new List<(string Label, bool Ok, string Detail)>
    {
        ("tool contract held", result.ToolContractHeld, string.Join(", ", result.ToolCalls)),
        ("extraction semantically correct", result.SemanticExtractionCorrect, string.Join("; ", result.SemanticShortfalls)),
        ("no verification failures", !result.Report.HasFailures, string.Join(", ", result.Report.RuleIdsWithStatus(VerificationStatus.Fail)))
    };

    foreach (var artifact in artifactNames)
    {
        var path = Path.Combine(outputDir, artifact);
        checks.Add(($"wrote {artifact}", File.Exists(path) && new FileInfo(path).Length > 0, path));
    }

    var consistent = ArtifactsAreConsistent(outputDir, out var inconsistency);
    checks.Add(("artifacts agree with each other", consistent, inconsistency));
    checks.Add(($"finished within {readyBudget}s", result.TotalSeconds <= readyBudget, $"{result.TotalSeconds}s"));

    foreach (var (label, ok, detail) in checks)
    {
        Console.WriteLine($"  {(ok ? "PASS" : "FAIL")}  {label}{(ok || detail.Length == 0 ? string.Empty : $"  [{detail}]")}");
    }

    var failed = checks.Count(c => !c.Ok);
    Console.WriteLine($"\nREADY: {(failed == 0 ? "PASS" : $"FAIL ({failed} check(s))")}");
    return failed == 0 ? 0 : 6;
}

int VerifyOnly()
{
    var ledgerPath = Option("--ledger") ?? Path.Combine(WorkshopPaths.Resolve(null, "artifacts"), "claim-ledger.json");
    if (!File.Exists(ledgerPath))
    {
        Console.Error.WriteLine($"no ledger at {ledgerPath}; run the pipeline first");
        return 3;
    }

    var clean = WorkshopJson.Deserialize<ClaimLedger>(File.ReadAllText(ledgerPath));
    var facts = SourceFactsParser.Parse(store);

    if (defect == SeededDefect.None)
    {
        var report = Verifier.Verify(clean, facts, store);
        WriteArtifacts(clean, report, BriefRenderer.Render(clean, report, facts));
        Console.WriteLine($"verification (no model): {report.Passed} passed, {report.Failed} failed, {report.Unverified} unverified");
        Console.WriteLine($"artifacts     : {outputDir}");
        return report.HasFailures ? 2 : 0;
    }

    // The corrupted trio is written together into its own directory, so the ledger, the report and
    // the brief always describe the same run and the clean evidence is never half-overwritten.
    var outcome = DefectInjector.Evaluate(defect, clean, facts, store);
    WriteArtifacts(outcome.Ledger, outcome.Report, outcome.Brief);

    Console.WriteLine($"seeded defect : {defect}");
    Console.WriteLine($"expected      : {Describe(defect)}");
    Console.WriteLine($"touched claims: {string.Join(", ", outcome.TouchedClaimIds)}");
    Console.WriteLine($"failed rules  : {(outcome.FailedRuleIds.Count == 0 ? "none" : string.Join(", ", outcome.FailedRuleIds))}");
    Console.WriteLine($"rejected      : {(outcome.Rejected ? "yes" : "NO")} (rule fired={outcome.RuleFiredAsIntended}, kept out of verified facts={outcome.KeptOutOfVerifiedFacts})");
    Console.WriteLine($"source ledger : {ledgerPath}");
    Console.WriteLine($"artifacts     : {outputDir}");

    if (!outcome.Rejected) return 4;
    return outcome.Report.HasFailures ? 2 : 0;
}

async Task<int> GatesAsync()
{
    var pipeline = new IncidentPipeline(settings, store);
    var gates = new Dictionary<string, string>(StringComparer.Ordinal);

    Console.WriteLine($"GATES | {settings.LaneLabel} | model={settings.Model} | endpoint={settings.Endpoint} | repeat={repeat} | budget={settings.RequestBudget.TotalSeconds:F0}s");

    var smokeText = await pipeline.SmokeAsync();
    var smokePassed = smokeText == "JACKDAW_OK";
    gates["L1-smoke"] = smokePassed ? "PASS" : "FAIL";
    Console.WriteLine($"  L1 smoke: {(smokePassed ? "PASS" : "FAIL")} [{smokeText}]");

    var runs = new List<object>();
    var durations = new List<double>();
    int semanticOk = 0, toolOk = 0, integratedOk = 0;
    PipelineResult? last = null;

    for (var i = 1; i <= repeat; i++)
    {
        var result = await pipeline.RunAsync();
        last = result;
        durations.Add(result.TotalSeconds);

        if (result.SemanticExtractionCorrect) semanticOk++;
        if (result.ToolContractHeld) toolOk++;
        if (!result.Report.HasFailures) integratedOk++;

        Console.WriteLine($"  run {i}: {result.TotalSeconds,5:F1}s tool={(result.ToolContractHeld ? "ok" : "BAD")} "
            + $"claims={result.Ledger.Claims.Count} pass={result.Report.Passed} fail={result.Report.Failed} unver={result.Report.Unverified}"
            + (result.Report.HasFailures ? $" [{string.Join(",", result.Report.RuleIdsWithStatus(VerificationStatus.Fail))}]" : string.Empty)
            + (result.SemanticExtractionCorrect ? string.Empty : $" semantic-gaps=[{string.Join("; ", result.SemanticShortfalls)}]"));

        runs.Add(new
        {
            index = i,
            seconds = result.TotalSeconds,
            gatherSeconds = result.GatherSeconds,
            extractSeconds = result.ExtractSeconds,
            toolCalls = result.ToolCalls,
            toolContractHeld = result.ToolContractHeld,
            typedExtractionValid = result.TypedExtractionValid,
            semanticExtractionCorrect = result.SemanticExtractionCorrect,
            semanticShortfalls = result.SemanticShortfalls,
            claims = result.Ledger.Claims.Count,
            passed = result.Report.Passed,
            failed = result.Report.Failed,
            unverified = result.Report.Unverified,
            failedRuleIds = result.Report.RuleIdsWithStatus(VerificationStatus.Fail)
        });
    }

    gates["L2-semantic-extraction"] = semanticOk == repeat ? "PASS" : "FAIL";
    gates["L3-tool-contract"] = toolOk == repeat ? "PASS" : "FAIL";
    gates["L4-integrated"] = integratedOk == repeat ? "PASS" : "FAIL";
    Console.WriteLine($"  L2 semantic extraction : {semanticOk}/{repeat}");
    Console.WriteLine($"  L3 tool contract       : {toolOk}/{repeat}");
    Console.WriteLine($"  L4 integrated          : {integratedOk}/{repeat}");

    // Every defect, three times each. One rejection is an anecdote.
    const int defectAttempts = 3;
    var defects = new List<object>();
    var facts = SourceFactsParser.Parse(store);
    var allRejected = true;

    foreach (var seeded in DefectInjector.All)
    {
        var outcomes = Enumerable.Range(1, defectAttempts)
            .Select(_ => DefectInjector.Evaluate(seeded, last!.Ledger, facts, store))
            .ToList();

        var rejected = outcomes.Count(o => o.Rejected);
        var expectation = DefectInjector.Expected(seeded);
        if (rejected != defectAttempts) allRejected = false;

        Console.WriteLine($"  defect {seeded,-17}: {rejected}/{defectAttempts} rejected  expected={expectation.RuleId}={expectation.Status} "
            + $"actual=[{string.Join(",", outcomes[0].FailedRuleIds)}]");

        defects.Add(new
        {
            defect = seeded.ToString(),
            expectedRuleId = expectation.RuleId,
            expectedStatus = expectation.Status.ToString(),
            attempts = defectAttempts,
            rejected,
            ruleFiredAsIntended = outcomes.All(o => o.RuleFiredAsIntended),
            keptOutOfVerifiedFacts = outcomes.All(o => o.KeptOutOfVerifiedFacts),
            touchedClaimIds = outcomes[0].TouchedClaimIds,
            failedRuleIds = outcomes[0].FailedRuleIds
        });
    }
    gates["L5-seeded-defects"] = allRejected ? "PASS" : "FAIL";

    durations.Sort();
    var median = durations[durations.Count / 2];
    var worst = durations[^1];
    var within30 = worst <= 30.0;
    var budgetSeconds = settings.RequestBudget.TotalSeconds;
    gates["L6-latency-30s"] = within30 ? "PASS" : "FAIL";
    gates["L6b-hard-fail-90s"] = worst > 90.0 ? "FAIL" : "PASS";
    Console.WriteLine($"  L6 latency             : median {median:F1}s worst {worst:F1}s (<=30s: {within30}); "
        + $"per-call ceiling {budgetSeconds:F0}s enforced by cancellation");

    var provenance = await Provenance.CaptureAsync(settings, TimeSpan.FromSeconds(10));
    Console.WriteLine($"  provenance             : {provenance.Model}@{provenance.ModelDigest} runtime {provenance.RuntimeVersion} | {provenance.Placement}");

    var allPassed = gates.Values.All(v => v == "PASS");
    var reportPath = Path.Combine(outputDir, "gate-report.json");
    File.WriteAllText(reportPath, WorkshopJson.Serialize(new
    {
        provenance,
        repeat,
        smoke = new { passed = smokePassed, text = smokeText },
        runs,
        defects,
        latency = new { medianSeconds = median, worstSeconds = worst, within30, perCallBudgetSeconds = budgetSeconds },
        gates,
        outcome = allPassed ? "PASS" : "FAIL"
    }));

    if (last is not null) WriteArtifacts(last.Ledger, last.Report, last.Brief);
    Console.WriteLine($"\ngate report: {reportPath}");
    Console.WriteLine($"OUTCOME: {(allPassed ? "PASS" : "FAIL")}");
    return allPassed ? 0 : 4;
}

void WriteArtifacts(ClaimLedger ledger, VerificationReport report, string brief)
{
    File.WriteAllText(Path.Combine(outputDir, "claim-ledger.json"), WorkshopJson.Serialize(ledger));
    File.WriteAllText(Path.Combine(outputDir, "verification.json"), WorkshopJson.Serialize(report));
    File.WriteAllText(Path.Combine(outputDir, "incident-brief.md"), brief);
}

/// <summary>Re-reads the three files from disk and checks they describe one run, not three.</summary>
bool ArtifactsAreConsistent(string directory, out string problem)
{
    problem = string.Empty;
    try
    {
        var ledger = WorkshopJson.Deserialize<ClaimLedger>(File.ReadAllText(Path.Combine(directory, "claim-ledger.json")));
        var report = WorkshopJson.Deserialize<VerificationReport>(File.ReadAllText(Path.Combine(directory, "verification.json")));
        var brief = File.ReadAllText(Path.Combine(directory, "incident-brief.md"));

        if (report.IncidentId != ledger.IncidentId)
        {
            problem = $"ledger says {ledger.IncidentId}, report says {report.IncidentId}";
            return false;
        }

        var unknown = report.Results
            .Select(r => r.ClaimId)
            .Where(id => id != VerificationRules.LedgerScope && ledger.Claims.All(c => c.Id != id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (unknown.Count > 0)
        {
            problem = $"report grades claims absent from the ledger: {string.Join(", ", unknown)}";
            return false;
        }

        foreach (var counter in new[] { $"- passed: {report.Passed}", $"- failed: {report.Failed}", $"- unverified: {report.Unverified}" })
        {
            if (!brief.Contains(counter, StringComparison.Ordinal))
            {
                problem = $"brief does not carry '{counter}'";
                return false;
            }
        }

        return true;
    }
    catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or InvalidOperationException)
    {
        problem = $"{ex.GetType().Name}: {ex.Message}";
        return false;
    }
}

string? Option(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static string DefaultOutputFor(SeededDefect defect) =>
    defect == SeededDefect.None ? "artifacts" : Path.Combine("artifacts", "break-it", DefectName(defect));

static string Describe(SeededDefect defect) =>
    $"{DefectInjector.Expected(defect).RuleId}={DefectInjector.Expected(defect).Status}";

static string DefectName(SeededDefect defect) => defect switch
{
    SeededDefect.PhantomSource => "phantom-source",
    SeededDefect.AlteredNumber => "altered-number",
    SeededDefect.AlteredTimestamp => "altered-timestamp",
    SeededDefect.MislabelledCause => "mislabelled-cause",
    SeededDefect.UnsupportedEvent => "unsupported-event",
    SeededDefect.SplicedQuote => "spliced-quote",
    _ => defect.ToString().ToLowerInvariant()
};

static SeededDefect ParseDefect(string? value) => value switch
{
    null or "none" => SeededDefect.None,
    "phantom-source" => SeededDefect.PhantomSource,
    "altered-number" => SeededDefect.AlteredNumber,
    "altered-timestamp" => SeededDefect.AlteredTimestamp,
    "mislabelled-cause" => SeededDefect.MislabelledCause,
    "unsupported-event" => SeededDefect.UnsupportedEvent,
    "spliced-quote" => SeededDefect.SplicedQuote,
    _ => throw new ArgumentException($"unknown defect '{value}'")
};

static int Usage()
{
    Console.WriteLine($"""
        Usage: dotnet run --project src/Workshop.App -- <command> [options]

        Commands:
          run           Full path: evidence tool -> typed extraction -> ledger -> verify -> brief
          smoke         One request; expects exactly JACKDAW_OK
          ready         One integrated run; checks all three artifacts, their consistency and the time budget
          gates         Local model gates L1-L6, writes artifacts/gate-report.json
          verify-only   Re-verify and re-render an existing ledger without calling the model

        Options:
          --evidence <dir>        default <repo>/evidence-pack
          --out <dir>             default <repo>/artifacts, or artifacts/break-it/<defect> when injecting
          --ledger <file>         ledger to re-verify, default <repo>/artifacts/claim-ledger.json
          --repeat <n>            gate repetitions, default 5
          --budget <seconds>      readiness wall-clock budget, default 90
          --inject-defect <name>  none | {string.Join(" | ", DefectInjector.All.Select(DefectName))}

        Environment:
          MAF_ENDPOINT, MAF_API_KEY, MAF_MODEL, MAF_TIMEOUT_SECONDS (per-call ceiling, default 90)

        Exit codes: 0 ok, 2 verification failed, 3 pipeline error, 4 gate failed,
                    5 tool contract broken, 6 readiness failed
        """);
    return 0;
}
