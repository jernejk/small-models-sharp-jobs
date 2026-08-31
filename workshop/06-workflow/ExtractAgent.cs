using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

internal static class ExtractAgent
{
    public static AIAgent Create(IChatClient client) => client.AsAIAgent(
        instructions: """
            Pick the records from the evidence pack that answer the question.
            Copy recordIds exactly as they appear in the pack. Never invent an id.
            Give a one-sentence rationale and a confidence between 0 and 100.
            Return only the typed contract. You have no tools.
            """,
        name: "ExtractAgent");

    /// <summary>The pack, not the corpus. Extract never sees a record Gather did not approve.</summary>
    public static string Prompt(string question, IReadOnlyList<CrashRecord> evidence) =>
        $"Question: {question}\nEvidence pack JSON:\n{Utilities.ToJson(evidence)}";
}
