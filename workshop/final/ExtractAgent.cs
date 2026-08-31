using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

internal static class ExtractAgent
{
    public static AIAgent Create(IChatClient client) => client.AsAIAgent(new ChatClientAgentOptions
    {
        Name = "ExtractAgent",
        ChatOptions = new ChatOptions
        {
            Temperature = 0,
            MaxOutputTokens = 700,
            // Without this the local model streams its reasoning into the reply and the typed
            // parse fails outright. Ollama hit that on ~40% of runs before it was set.
            Reasoning = new ReasoningOptions { Effort = ReasoningEffort.None, Output = ReasoningOutput.None },
            Instructions = """
            Pick the records from the evidence pack that answer the question.
            Copy recordIds exactly as they appear in the pack. Never invent an id.
            Give a one-sentence rationale and a confidence between 0 and 100.
            Return only the typed contract. You have no tools.
            """
        }
    });

    /// <summary>The pack, not the corpus. Extract never sees a record Gather did not approve.</summary>
    public static string Prompt(string question, IReadOnlyList<CrashRecord> evidence) =>
        $"Question: {question}\nEvidence pack JSON:\n{Utilities.ToJson(evidence)}";
}
