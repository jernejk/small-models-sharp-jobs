using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json.Nodes;

namespace Workshop.App;

/// <summary>Ollama's /v1 drops <c>max_completion_tokens</c> and honours only legacy <c>max_tokens</c>; both are sent because LM Studio and OpenAI need the modern spelling.</summary>
internal sealed class MaxTokensCompatibilityPolicy : PipelinePolicy
{
    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int index)
    {
        AddLegacyCap(message.Request);
        ProcessNext(message, pipeline, index);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int index)
    {
        AddLegacyCap(message.Request);
        await ProcessNextAsync(message, pipeline, index);
    }

    private static void AddLegacyCap(PipelineRequest request)
    {
        if (request.Content is null
            || !string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase)
            || request.Uri?.AbsolutePath.EndsWith("chat/completions", StringComparison.Ordinal) is not true) return;

        using var buffer = new MemoryStream();
        request.Content.WriteTo(buffer);
        if (JsonNode.Parse(buffer.ToArray()) is not JsonObject body
            || body["max_completion_tokens"] is not JsonNode cap
            || body.ContainsKey("max_tokens")) return;

        body["max_tokens"] = cap.DeepClone();
        request.Content = BinaryContent.Create(BinaryData.FromString(body.ToJsonString()));
    }
}
