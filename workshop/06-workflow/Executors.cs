using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

/// <summary>What travels along the edges. A non-null Gate means "stop and report".</summary>
internal sealed record CrashRun(
    IReadOnlyList<CrashRecord> Evidence,
    CrashSelection? Selection,
    IReadOnlyList<CrashRecord> Selected,
    CrashAnalysis? Analysis,
    CrashGate? Gate);

/// <summary>Reasoning off and a hard token cap. Without these the local model streams its reasoning
/// into the reply and the typed parse fails outright — about 40% of runs on Ollama.</summary>
internal static class Agents
{
    public static AIAgent Create(IChatClient client, string name, string instructions) =>
        client.AsAIAgent(new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions
            {
                Temperature = 0,
                MaxOutputTokens = 700,
                Reasoning = new ReasoningOptions { Effort = ReasoningEffort.None, Output = ReasoningOutput.None },
                Instructions = instructions
            }
        });
}

internal sealed class GatherExecutor(IChatClient client, IReadOnlyList<CrashRecord> records)
    : Executor<string, CrashRun>("gather")
{
    private readonly AIAgent _agent = Agents.Create(client, "GatherAgent", """
        Turn the prompt into a date range, term and result cap.
        The records are Victorian road crashes from 2012 to 2025.
        Dates are yyyy-MM-dd. Leave from and to empty unless the prompt names a year or a range.
        Term is one or two words naming the topic, or empty if the prompt names none.
        Return only the typed contract. You have no tools.
        """);

    public override async ValueTask<CrashRun> HandleAsync(string prompt, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var query = await _agent.RunAsync<QueryFilter>(prompt, cancellationToken: cancellationToken);
        var evidence = Utilities.Gather(records, Utilities.ValidateFilter(Gates.TryTyped(query)));
        return new CrashRun(evidence, null, [], null, evidence.Count == 0 ? CrashGate.NoEvidence : null);
    }
}

internal sealed class ExtractExecutor(IChatClient client, string prompt)
    : Executor<CrashRun, CrashRun>("extract")
{
    private readonly AIAgent _agent = Agents.Create(client, "ExtractAgent", """
        Pick the records from the evidence pack that answer the question.
        Copy recordIds exactly as they appear in the pack. Never invent an id.
        Give a one-sentence rationale and a confidence between 0 and 100.
        Return only the typed contract. You have no tools.
        """);

    public override async ValueTask<CrashRun> HandleAsync(CrashRun run, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // The pack, not the corpus. Extract never sees a record Gather did not approve.
        var extract = await _agent.RunAsync<CrashSelection>(
            $"Question: {prompt}\nEvidence pack JSON:\n{Utilities.ToJson(run.Evidence)}", cancellationToken: cancellationToken);

        var selection = Gates.TryTyped(extract);
        var gate = Gates.ValidateSelection(run.Evidence, selection, out var selected);
        return run with { Selection = selection, Selected = selected, Gate = gate is CrashGate.Supported ? null : gate };
    }
}

internal sealed class AnalyseExecutor(IChatClient client, string prompt)
    : Executor<CrashRun, CrashRun>("analyse")
{
    private readonly AIAgent _agent = Agents.Create(client, "AnalyseAgent", """
        Analyse only the records you are given. Return a grounded finding, practical actions,
        open questions, and a confidence between 0 and 100.
        Do not claim a cause the records do not state. Do not mention records you were not given.
        Return only the typed contract. You have no tools.
        """);

    public override async ValueTask<CrashRun> HandleAsync(CrashRun run, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // Only what the selection gate cleared. The evidence pack does not come along.
        var analyse = await _agent.RunAsync<CrashAnalysis>(
            $"Question: {prompt}\nValidated selected records JSON:\n{Utilities.ToJson(run.Selected)}", cancellationToken: cancellationToken);

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
