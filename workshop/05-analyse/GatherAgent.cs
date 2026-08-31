using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

internal static class GatherAgent
{
    public static AIAgent Create(IChatClient client) => client.AsAIAgent(
        instructions: """
            Turn the prompt into a date range, term and result cap.
            The records are Victorian road crashes from 2012 to 2025.
            Dates are yyyy-MM-dd. Leave from and to empty unless the prompt names a year or a range.
            A single year is a range: its first day through its last day.
            Term is one or two words naming the topic, or empty if the prompt names none.
            Return only the typed contract. You have no tools.
            """,
        name: "GatherAgent");
}
