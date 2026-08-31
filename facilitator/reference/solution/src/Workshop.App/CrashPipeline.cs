using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Workshop.Core;

namespace Workshop.App;

internal sealed record CrashRun(EvidencePack Evidence, CrashSelection? Selection, CrashAnalysis? Analysis, CrashGate Gate, string Message);

internal sealed record HelloCheck(string Reply, bool TokenPresent, TimeSpan Elapsed);

/// <summary>The smallest typed contract that still has something to validate: text plus a bounded number.</summary>
internal sealed record SimpleCommand(string Action, string Target);

internal sealed record TypedCheck(string Raw, SimpleCommand? Value, TimeSpan Elapsed)
{
    public bool IsValid => Value is not null && !string.IsNullOrWhiteSpace(Value.Action) && !string.IsNullOrWhiteSpace(Value.Target);
}

internal sealed record QueryCheck(string Raw, QueryFilter? Value, TimeSpan Elapsed)
{
    public bool IsValid => Value is not null;
}

/// <summary>Explicit, code-owned workflow. Gather is deterministic; each typed model call is tool-free.</summary>
internal sealed class CrashPipeline(ModelSettings settings)
{
    private readonly IChatClient _chat = CreateChatClient(settings);

    private static IChatClient CreateChatClient(ModelSettings settings)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(settings.Endpoint.TrimEnd('/') + "/"), NetworkTimeout = settings.RequestBudget,
            RetryPolicy = new ClientRetryPolicy(maxRetries: 0)
        };
        options.AddPolicy(new MaxTokensCompatibilityPolicy(), PipelinePosition.PerCall);
        return new OpenAIClient(new ApiKeyCredential(settings.ApiKey), options).GetChatClient(settings.Model).AsIChatClient();
    }

    public const string HelloToken = "WORKSHOP_OK";

    /// <summary>CP-01: one plain chat call, no tools and no typed contract, so a failure can only be the endpoint.</summary>
    public async Task<HelloCheck> HelloAsync(CancellationToken cancellationToken = default)
    {
        var agent = Agent("HelloAgent", "Reply with exactly the token you are given. No explanation, no punctuation, no other words. You have no tools.");
        var started = Stopwatch.GetTimestamp();
        var response = await agent.RunAsync($"Reply with exactly this token and nothing else: {HelloToken}", cancellationToken: cancellationToken);
        var reply = response.Text.Trim();
        return new HelloCheck(reply, reply.Contains(HelloToken, StringComparison.Ordinal), Stopwatch.GetElapsedTime(started));
    }

    /// <summary>CP-02: the structured-output path Extract uses, on a contract small enough to read at a glance.</summary>
    public async Task<TypedCheck> TypedAsync(CancellationToken cancellationToken = default)
    {
        var agent = Agent("TypedAgent", """
            Convert the request into one small command with an action and target.
            Return only the typed contract. You have no tools.
            """);
        var started = Stopwatch.GetTimestamp();
        var response = await agent.RunAsync<SimpleCommand>("Mute the microphone.", cancellationToken: cancellationToken);
        SimpleCommand? value;
        try { value = response.Result; }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException) { value = null; }
        return new TypedCheck(response.Text, value, Stopwatch.GetElapsedTime(started));
    }

    /// <summary>CP-03: a tool-free model call narrows a natural-language question; C# owns the resulting bounds.</summary>
    public async Task<QueryCheck> InterpretQueryAsync(string question, CancellationToken cancellationToken = default)
    {
        var agent = Agent("QueryAgent", """
            Turn the user's incident-search question into an optional date range, a concise search term, and a maximum result count.
            Use only facts stated in the question. Dates must be full ISO yyyy-MM-dd values: turn a whole year into its first or last day.
            Leave fields null when absent; choose 8 when no result count is requested.
            Return only the typed contract. You have no tools and cannot access records.
            """);
        var started = Stopwatch.GetTimestamp();
        var response = await agent.RunAsync<QueryFilter>(question, cancellationToken: cancellationToken);
        QueryFilter? value;
        try { value = response.Result; }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException) { value = null; }
        return new QueryCheck(response.Text, value, Stopwatch.GetElapsedTime(started));
    }

    public async Task<CrashRun> RunAsync(string question, EvidencePack evidence, CancellationToken cancellationToken = default)
    {
        if (evidence.IsEmpty)
            return new(evidence, null, null, CrashGate.NoEvidence, "No matching records. Extract and Analyse were not called.");

        var selection = await ExtractAsync(question, evidence, cancellationToken);
        var selectionGate = CrashWorkflow.ValidateSelection(evidence, selection, out var selected);
        if (selectionGate is CrashGate.UnsupportedSelection or CrashGate.LowConfidence)
            return new(evidence, selection, null, selectionGate, "Extraction was not supported strongly enough; code stopped before Analyse.");

        var analysis = await AnalyseAsync(question, selected, cancellationToken);
        var analysisGate = CrashWorkflow.ValidateAnalysis(selected, analysis);
        return new(evidence, selection, analysis, analysisGate,
            analysisGate == CrashGate.Supported ? "Supported finding: display the grounded analysis." : "Analysis was not supported strongly enough; display the caution branch.");
    }

    private async Task<CrashSelection?> ExtractAsync(string question, EvidencePack evidence, CancellationToken token)
    {
        var agent = Agent("ExtractAgent", """
            Select only relevant records from the provided evidence pack for the user's question.
            Return recordIds from the pack exactly, a short rationale, and 0-100 confidence.
            Do not invent records, facts, causes, or recommendations. You have no tools.
            """);
        var response = await agent.RunAsync<CrashSelection>($"Question: {question}\nEvidence pack JSON:\n{WorkshopJson.Serialize(evidence)}", cancellationToken: token);
        try { return response.Result; }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException) { return null; }
    }

    private async Task<CrashAnalysis?> AnalyseAsync(string question, IReadOnlyList<IncidentRecord> selected, CancellationToken token)
    {
        var agent = Agent("AnalyseAgent", """
            Analyse only the compact structured records provided. Return a grounded finding, practical actions,
            open questions, and 0-100 confidence. Do not claim causation not present in the records.
            """);
        var response = await agent.RunAsync<CrashAnalysis>($"Question: {question}\nValidated selected records JSON:\n{WorkshopJson.Serialize(selected)}", cancellationToken: token);
        try { return response.Result; }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException) { return null; }
    }

    private AIAgent Agent(string name, string instructions) => _chat.AsAIAgent(new ChatClientAgentOptions
    {
        Name = name,
        ChatOptions = new ChatOptions { Instructions = instructions, Temperature = 0, MaxOutputTokens = 700, Reasoning = new ReasoningOptions { Effort = ReasoningEffort.None, Output = ReasoningOutput.None } }
    });
}
