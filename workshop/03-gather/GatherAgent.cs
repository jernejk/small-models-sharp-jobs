using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

internal static class GatherAgent
{
    public static AIAgent Create(IChatClient client) => client.AsAIAgent(
        instructions: """
            Turn the prompt into a date range, term and result cap.
            Return only the typed contract. You have no tools.
            """,
        name: "GatherAgent");
}
