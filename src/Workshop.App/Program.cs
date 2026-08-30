using System.ClientModel;
using System.Globalization;
using Workshop.App;
using Workshop.Core;

var command = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? "run";
var dataset = WorkshopPaths.Resolve(Option("--dataset"), "workshop/data/victoria-road-crash-sample.json");
var question = Option("--question") ?? "What patterns should an insurance and safety analyst review in the selected crash records?";
var from = ParseDate(Option("--from"));
var to = ParseDate(Option("--to"));
var term = Option("--term");
var max = ParseMax(Option("--max"));

if (from is not null && to is not null && from > to)
    return Error("--from must be on or before --to");

var evidence = IncidentDataset.Gather(IncidentDataset.Load(dataset), new IncidentQuery(from, to, term, max));

try
{
    return command switch
    {
        "gather" => PrintGather(),
        "smoke" => await SmokeAsync(),
        "typed" => await TypedAsync(),
        "run" => await RunAsync("explicit calls"),
        "workflow" => await RunAsync("linear workflow"),
        "ready" => await ReadyAsync(),
        _ => Usage()
    };
}
catch (Exception ex)
{
    Console.Error.WriteLine($"pipeline error: {Classify(ex) ?? $"{ex.GetType().Name}: {ex.Message}"}");
    return 3;
}

int PrintGather()
{
    Console.WriteLine(WorkshopJson.Serialize(evidence));
    Console.Error.WriteLine(evidence.IsEmpty
        ? "GATHER: no matching evidence in the approved Victorian crash sample. Extract and Analyse stop here."
        : $"GATHER: {evidence.Records.Count} bounded record(s). Pass this compact pack, not the dataset, to Extract.");
    return 0;
}

async Task<int> SmokeAsync()
{
    var settings = ModelSettings.FromEnvironment();
    Console.WriteLine($"local hello | {settings.LaneLabel} | endpoint={settings.Endpoint} | model={settings.Model}");
    HelloCheck check;
    try { check = await new CrashPipeline(settings).HelloAsync(); }
    catch (Exception ex) { return ModelError("smoke", ex); }

    Console.WriteLine($"reply: {check.Reply}");
    Console.WriteLine($"elapsed: {check.Elapsed.TotalMilliseconds:F0} ms");
    if (check.TokenPresent) return 0;
    return ModelHint("smoke", $"wrong reply: expected {CrashPipeline.HelloToken}; check MAF_MODEL names a loaded chat model");
}

async Task<int> TypedAsync()
{
    var settings = ModelSettings.FromEnvironment();
    Console.WriteLine($"typed json | {settings.LaneLabel} | endpoint={settings.Endpoint} | model={settings.Model}");
    TypedCheck check;
    try { check = await new CrashPipeline(settings).TypedAsync(); }
    catch (Exception ex) { return ModelError("typed", ex); }

    Console.WriteLine("raw:\n" + check.Raw);
    if (!check.IsValid)
        return ModelHint("typed", "malformed output: the reply did not parse into { greeting, confidence 0-100 }");

    Console.WriteLine($"greeting: {check.Value!.Greeting}");
    Console.WriteLine($"confidence: {check.Value.Confidence}");
    Console.WriteLine($"elapsed: {check.Elapsed.TotalMilliseconds:F0} ms");
    return 0;
}

async Task<int> RunAsync(string shape)
{
    var settings = ModelSettings.FromEnvironment();
    Console.WriteLine($"{shape} | {settings.LaneLabel} | model={settings.Model}");
    Console.WriteLine($"question: {question}");
    Console.WriteLine($"gathered: {evidence.Records.Count} record(s)");
    var result = await new CrashPipeline(settings).RunAsync(question, evidence);
    Console.WriteLine($"gate: {result.Gate}");
    Console.WriteLine(result.Message);
    if (result.Selection is not null) Console.WriteLine("extract JSON:\n" + WorkshopJson.Serialize(result.Selection));
    if (result.Analysis is not null) Console.WriteLine("analyse JSON:\n" + WorkshopJson.Serialize(result.Analysis));
    return result.Gate is CrashGate.Supported or CrashGate.NoEvidence ? 0 : 2;
}

async Task<int> ReadyAsync()
{
    if (evidence.IsEmpty) return Error("ready needs a matching deterministic Gather result; choose a date or term that returns records.");
    var exit = await RunAsync("readiness rehearsal");
    Console.WriteLine(exit == 0 ? "READY: model-backed supported path completed." : "READY: not ready; inspect the gate before presenting.");
    return exit;
}

string? Option(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static DateOnly? ParseDate(string? value) => string.IsNullOrWhiteSpace(value) ? null
    : DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
        ? date : throw new ArgumentException($"invalid date '{value}'; use yyyy-MM-dd");

static int ParseMax(string? value) => int.TryParse(value, CultureInfo.InvariantCulture, out var parsed) ? Math.Clamp(parsed, 1, 20) : 8;
static int Error(string message) { Console.Error.WriteLine($"error: {message}"); return 3; }
static int ModelHint(string command, string hint) { Console.Error.WriteLine($"{command}: {hint}"); return 2; }

static int ModelError(string command, Exception error) =>
    ModelHint(command, Classify(error) ?? $"{error.GetType().Name}: {error.Message}");

static string? Classify(Exception error)
{
    for (Exception? current = error; current is not null; current = current.InnerException)
    {
        if (current is HttpRequestException) return "endpoint down: nothing answered MAF_ENDPOINT";
        if (current is ClientResultException { Status: 404 }) return "model not loaded: MAF_MODEL is not served by that endpoint";
        if (current is TaskCanceledException) return "timed out: raise MAF_TIMEOUT_SECONDS or load a smaller model";
        if (current is ArgumentOutOfRangeException || current.Message.Contains("choices", StringComparison.OrdinalIgnoreCase))
            return "provider returned no answer (upstream overloaded): retry, or switch endpoint";
    }
    return null;
}

static int Usage()
{
    Console.WriteLine("""
        Usage: dotnet run --project src/Workshop.App -- <command> [options]
          smoke     CP-01 one plain no-tool chat call that must echo an exact token
          typed     CP-02 one no-tool call that must return a small typed JSON contract
          gather    deterministic bounded Gather over the approved Victorian crash sample
          run       explicit Gather -> Extract -> Analyse calls with code-owned gates
          workflow  the same safe linear sequence, labelled as the reusable workflow step
          ready     model-backed rehearsal of a supported path
        Options: --from yyyy-MM-dd --to yyyy-MM-dd --term text --max 1..20 --question text --dataset file
        Environment: MAF_ENDPOINT, MAF_API_KEY, MAF_MODEL, MAF_TIMEOUT_SECONDS
        """);
    return 0;
}
