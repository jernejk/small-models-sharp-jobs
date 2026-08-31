/// <summary>One de-identified Victorian crash record from the approved local sample.</summary>
internal sealed record CrashRecord(string Id, DateOnly Date, string Title, string Summary, string Severity, string SourceReference);

/// <summary>What the model produces. Dates are plain strings here because a model will happily
/// answer "2012", "last year" or "" — untrusted means untrusted, including the type.</summary>
internal sealed record QueryFilter(string? From, string? To, string? Term, int? MaxResults);

/// <summary>The validated filter. Only this shape is ever allowed near the corpus.</summary>
internal sealed record CrashQuery(DateOnly? From, DateOnly? To, string? Term, int MaxResults);

/// <summary>What Extract produces. The IDs are claims about the evidence pack until code checks them.</summary>
internal sealed record CrashSelection(string[] RecordIds, string Rationale, int Confidence);

internal sealed record ModelSettings(string Endpoint, string ApiKey, string Model)
{
    public string Lane => Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) && uri.IsLoopback ? "LOCAL" : "HOSTED";
}
