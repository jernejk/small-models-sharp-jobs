using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

/// <summary>What travels along the edges. A non-null Gate means "stop and report".</summary>
internal sealed record CrashRun(
    IReadOnlyList<CrashRecord> Evidence,
    CrashSelection? Selection,
    IReadOnlyList<CrashRecord> Selected,
    CrashAnalysis? Analysis,
    CrashGate? Gate);

internal sealed class GatherExecutor(IChatClient client, IReadOnlyList<CrashRecord> records)
    : Executor<string, CrashRun>("gather")
{
    public override async ValueTask<CrashRun> HandleAsync(string prompt, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var query = await GatherAgent.Create(client).RunAsync<QueryFilter>(prompt, cancellationToken: cancellationToken);
        var evidence = Utilities.Gather(records, Utilities.ValidateFilter(Gates.TryTyped(query)));
        return new CrashRun(evidence, null, [], null, evidence.Count == 0 ? CrashGate.NoEvidence : null);
    }
}

internal sealed class ExtractExecutor(IChatClient client, string prompt)
    : Executor<CrashRun, CrashRun>("extract")
{
    public override async ValueTask<CrashRun> HandleAsync(CrashRun run, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var extract = await ExtractAgent.Create(client).RunAsync<CrashSelection>(ExtractAgent.Prompt(prompt, run.Evidence), cancellationToken: cancellationToken);
        var selection = Gates.TryTyped(extract);
        var gate = Gates.ValidateSelection(run.Evidence, selection, out var selected);
        return run with { Selection = selection, Selected = selected, Gate = gate is CrashGate.Supported ? null : gate };
    }
}

internal sealed class AnalyseExecutor(IChatClient client, string prompt)
    : Executor<CrashRun, CrashRun>("analyse")
{
    public override async ValueTask<CrashRun> HandleAsync(CrashRun run, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var analyse = await AnalyseAgent.Create(client).RunAsync<CrashAnalysis>(AnalyseAgent.Prompt(prompt, run.Selected), cancellationToken: cancellationToken);
        var analysis = Gates.TryTyped(analyse);
        return run with { Analysis = analysis, Gate = Gates.ValidateAnalysis(run.Selected, analysis) };
    }
}

internal sealed class ReportExecutor() : Executor<CrashRun, CrashGate>("report")
{
    public override ValueTask<CrashGate> HandleAsync(CrashRun run, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"gathered: {run.Evidence.Count} record(s)");
        if (run.Selection is not null) Console.WriteLine("selected: " + Utilities.ToJson(run.Selected));
        if (run.Analysis is not null) Console.WriteLine("analysis: " + Utilities.ToJson(run.Analysis));
        Console.WriteLine($"gate: {run.Gate}");
        return ValueTask.FromResult(run.Gate!.Value);
    }
}
