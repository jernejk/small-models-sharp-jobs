/// <summary>One de-identified Victorian crash record from the approved local sample.</summary>
internal sealed record CrashRecord(string Id, DateOnly Date, string Title, string Summary, string Severity, string SourceReference);

/// <summary>What the model produces from the prompt. Untrusted until C# validates it.</summary>
internal sealed record QueryFilter(DateOnly? From, DateOnly? To, string? Term, int? MaxResults);

internal sealed record ModelSettings(string Endpoint, string ApiKey, string Model)
{
    public string Lane => Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) && uri.IsLoopback ? "LOCAL" : "HOSTED";
}
