using System.ClientModel;
using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Workshop.Core;

namespace Workshop.App;

internal sealed class ModelBudgetExceededException(TimeSpan budget)
    : Exception($"the model did not answer inside the {budget.TotalSeconds:F0}s request budget (MAF_TIMEOUT_SECONDS)");

internal sealed record PipelineResult(
    ClaimLedger Ledger,
    VerificationReport Report,
    string Brief,
    IReadOnlyList<string> ToolCalls,
    bool ToolContractHeld,
    bool TypedExtractionValid,
    double GatherSeconds,
    double ExtractSeconds,
    double TotalSeconds)
{
    /// <summary>
    /// L2. Parseable JSON containing an incident id proves almost nothing; this asks whether the
    /// extraction actually got the incident right and quoted it honestly.
    /// </summary>
    public bool SemanticExtractionCorrect =>
        TypedExtractionValid
        && Ledger.Claims.Count > 0
        && Passes(VerificationRules.RequiredClaims)
        && Passes(VerificationRules.IncidentId)
        && Passes(VerificationRules.Severity)
        && Passes(VerificationRules.AffectedCustomers)
        && NoFailure(VerificationRules.QuotePresent)
        && NoFailure(VerificationRules.KindSemantics);

    public IReadOnlyList<string> SemanticShortfalls =>
    [
        .. new (string Label, bool Ok)[]
        {
            ("every prose source produced claims", TypedExtractionValid),
            ("ledger is not empty", Ledger.Claims.Count > 0),
            ("all required kinds present", Passes(VerificationRules.RequiredClaims)),
            ("incident id correct", Passes(VerificationRules.IncidentId)),
            ("severity correct", Passes(VerificationRules.Severity)),
            ("affected customers correct", Passes(VerificationRules.AffectedCustomers)),
            ("every quote real", NoFailure(VerificationRules.QuotePresent)),
            ("no kind mislabelled", NoFailure(VerificationRules.KindSemantics))
        }.Where(check => !check.Ok).Select(check => check.Label)
    ];

    private bool Passes(string ruleId) =>
        Report.Results.Any(r => r.RuleId == ruleId && r.Status == VerificationStatus.Pass) && NoFailure(ruleId);

    private bool NoFailure(string ruleId) =>
        !Report.Results.Any(r => r.RuleId == ruleId && r.Status == VerificationStatus.Fail);
}

/// <summary>The model output the extraction step is allowed to return. Nothing else reaches the ledger.</summary>
internal sealed record SourceExtraction(ExtractedClaimDto[] Claims);

internal sealed record ExtractedClaimDto(string Kind, string Value, string ExactQuote);

internal sealed class IncidentPipeline(ModelSettings settings, EvidenceStore store)
{
    /// <summary>
    /// Only the prose files go to the model. events.csv is structured data that ordinary code
    /// parses exactly, and sending it would cost roughly ten seconds for a worse answer.
    /// </summary>
    public static readonly IReadOnlyList<string> ProseSources = ["status.txt", "customer-email.txt"];

    private const string GatherInstructions = """
        You fetch incident evidence using the read_evidence tool.
        Call read_evidence exactly once for each file you are asked for, using the file name verbatim.
        Do not summarise or quote the contents. When every file has been read, reply with exactly DONE.
        """;

    private const string ExtractInstructions = """
        You extract factual claims from ONE incident evidence file.

        Rules:
        - exactQuote must be copied character-for-character from ONE line of that file. Never paraphrase it,
          and never join text from two different lines into one quote.
        - value must be the bare fact with no surrounding words.
        - Only emit a claim you can quote literally from this file. Never compute, derive or total a value.
        - If this file contains no fact of a given kind, do not emit that kind. Never carry facts over from another file.
        - Emit at most 5 claims and never repeat one.

        What each kind means:
        - incident_id: an incident identifier such as INC-042
        - severity: a severity label such as SEV-2
        - affected_customers: how many customers were affected, digits only
        - timestamp: one complete ISO-8601 timestamp
        - duration: a length of time
        - event: something that happened during the incident
        - cause: a stated or suspected reason WHY the incident happened

        Anything saying why the incident happened - "caused by", "due to", "because", "triggered by" -
        is kind cause, never kind event, even when someone is only guessing.
        """;

    private readonly IChatClient _chatClient = new OpenAIClient(
            new ApiKeyCredential(settings.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(settings.Endpoint.TrimEnd('/') + "/"),
                NetworkTimeout = settings.RequestBudget
            })
        .GetChatClient(settings.Model)
        .AsIChatClient();

    public ModelSettings Settings => settings;

    public async Task<string> SmokeAsync()
    {
        var agent = CreateAgent("Smoke", "Follow the user's output-format instruction exactly.");
        var response = await WithBudgetAsync(token => agent.RunAsync("Reply with exactly JACKDAW_OK and nothing else.", cancellationToken: token));
        return response.Text.Trim();
    }

    /// <summary>
    /// A hard ceiling on every model call. Without it a stalled runtime hangs the workshop instead
    /// of failing, and the >90 s gate could only ever report a breach it had already sat through.
    /// </summary>
    private async Task<T> WithBudgetAsync<T>(Func<CancellationToken, Task<T>> call)
    {
        using var budget = new CancellationTokenSource(settings.RequestBudget);
        try
        {
            return await call(budget.Token);
        }
        catch (OperationCanceledException) when (budget.IsCancellationRequested)
        {
            throw new ModelBudgetExceededException(settings.RequestBudget);
        }
    }

    public async Task<PipelineResult> RunAsync(SeededDefect defect = SeededDefect.None)
    {
        var total = Stopwatch.StartNew();
        var host = new EvidenceToolHost(store);

        var gatherWatch = Stopwatch.StartNew();
        var toolContractHeld = await GatherAsync(host);
        gatherWatch.Stop();

        var extractWatch = Stopwatch.StartNew();
        var extracted = new List<ExtractedClaim>();
        var typedExtractionValid = true;

        foreach (var sourceId in ProseSources)
        {
            if (!host.Fetched.TryGetValue(sourceId, out var content))
            {
                typedExtractionValid = false;
                continue;
            }

            // A fresh agent per source: a reused one carried status.txt facts into the email extraction.
            var claims = await ExtractAsync(CreateAgent("Extractor", ExtractInstructions), sourceId, content);
            if (claims.Count == 0) typedExtractionValid = false;
            extracted.AddRange(claims);
        }
        extractWatch.Stop();

        var facts = SourceFactsParser.Parse(store);
        var ledger = DefectInjector.Inject(LedgerBuilder.Build(extracted), defect);
        var report = Verifier.Verify(ledger, facts, store);
        var brief = BriefRenderer.Render(ledger, report, facts);
        total.Stop();

        return new PipelineResult(
            ledger, report, brief,
            [.. host.Calls],
            toolContractHeld,
            typedExtractionValid,
            Math.Round(gatherWatch.Elapsed.TotalSeconds, 2),
            Math.Round(extractWatch.Elapsed.TotalSeconds, 2),
            Math.Round(total.Elapsed.TotalSeconds, 2));
    }

    /// <summary>Returns true when the agent asked for exactly the requested files, once each.</summary>
    private async Task<bool> GatherAsync(EvidenceToolHost host)
    {
        var tool = CreateEvidenceTool(host);
        var agent = CreateAgent("Gatherer", GatherInstructions, [tool]);
        var request = $"Call read_evidence for each of these files: {string.Join(", ", ProseSources)}.";

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            host.Reset();
            await WithBudgetAsync(token => agent.RunAsync(request, cancellationToken: token));

            var askedForExactly = host.Calls.Count == ProseSources.Count
                && host.Calls.Order(StringComparer.Ordinal).SequenceEqual(ProseSources.Order(StringComparer.Ordinal), StringComparer.Ordinal);

            if (askedForExactly && ProseSources.All(host.Fetched.ContainsKey))
            {
                return true;
            }
        }

        return false;
    }

    private static AITool CreateEvidenceTool(EvidenceToolHost host)
    {
        // >>> TODO 2 | Expose the evidence reader to the agent as a tool named read_evidence. | STUB: throw new NotImplementedException("TODO 2: register the evidence tool");
        return AIFunctionFactory.Create(
            host.ReadEvidence,
            name: "read_evidence",
            description: "Read one whitelisted evidence file from the incident pack. Returns its full text.");
        // <<< TODO 2
    }

    private async Task<IReadOnlyList<ExtractedClaim>> ExtractAsync(AIAgent agent, string sourceId, string content)
    {
        var prompt = $"""
            sourceId: {sourceId}
            Allowed kind values: {string.Join(", ", ClaimKinds.All)}.
            File content:
            {content}
            """;

        // >>> TODO 3 | Ask the agent for typed claims and attach the source id we already know. | STUB: throw new NotImplementedException("TODO 3: run the typed extraction step");
        var response = await WithBudgetAsync(token => agent.RunAsync<SourceExtraction>(prompt, cancellationToken: token));
        return
        [
            .. (response.Result?.Claims ?? [])
                .Where(c => !string.IsNullOrWhiteSpace(c.Value))
                .Select(c => new ExtractedClaim(sourceId, c.Kind ?? string.Empty, c.Value, c.ExactQuote ?? string.Empty))
        ];
        // <<< TODO 3
    }

    private AIAgent CreateAgent(string name, string instructions, IList<AITool>? tools = null) =>
        _chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions
            {
                Instructions = instructions,
                Temperature = 0,
                Tools = tools,
                // Reasoning-off is a compatibility invariant: with default reasoning this model
                // returned an empty visible answer after 57.7 s on the reference machine.
                Reasoning = new ReasoningOptions
                {
                    Effort = ReasoningEffort.None,
                    Output = ReasoningOutput.None
                }
            }
        });
}
