using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

internal static class AnalyseAgent
{
    public static AIAgent Create(IChatClient client) => client.AsAIAgent(
        instructions: """
            Analyse only the records you are given. Return a grounded finding, practical actions,
            open questions, and a confidence between 0 and 100.
            Do not claim a cause the records do not state. Do not mention records you were not given.
            Return only the typed contract. You have no tools.
            """,
        name: "AnalyseAgent");

    /// <summary>Only the records the selection gate cleared. The evidence pack does not come along.</summary>
    public static string Prompt(string question, IReadOnlyList<CrashRecord> selected) =>
        $"Question: {question}\nValidated selected records JSON:\n{Utilities.ToJson(selected)}";
}
