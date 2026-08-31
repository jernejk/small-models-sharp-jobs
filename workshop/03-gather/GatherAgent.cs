using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

internal static class GatherAgent
{
    public static AIAgent Create(IChatClient client) => client.AsAIAgent(new ChatClientAgentOptions
    {
        Name = "GatherAgent",
        ChatOptions = new ChatOptions
        {
            Temperature = 0,
            MaxOutputTokens = 700,
            // Without this the local model streams its reasoning into the reply and the typed
            // parse fails outright. Ollama hit that on ~40% of runs before it was set.
            Reasoning = new ReasoningOptions { Effort = ReasoningEffort.None, Output = ReasoningOutput.None },
            Instructions = """
            Turn the prompt into a date range, term and result cap.
            The records are Victorian road crashes from 2012 to 2025.
            Dates are yyyy-MM-dd. Leave from and to empty unless the prompt names a year or a range.
            Term is one or two words naming the topic, or empty if the prompt names none.
            Return only the typed contract. You have no tools.
            """
        }
    });
}
