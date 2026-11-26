using System.Globalization;
using Runesvaner;
using Runesvaner.Storage.Jsonl;

var command = args.Length > 0 ? args[0] : "report";

return command switch
{
    "report" => Report(ParsePath(args)),
    "doctor" => Doctor(ParsePath(args)),
    "costs" => Costs(ParsePath(args)),
    "pricing" => Pricing(args),
    "run" => RunNotImplemented(),
    "--help" or "-h" or "help" => Help(),
    _ => Unknown(command)
};

static string ParsePath(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], "--path", StringComparison.Ordinal))
        {
            return args[i + 1];
        }
    }

    return ".runesvaner";
}

static int Report(string path)
{
    var records = JsonlRunesvanerStore.ReadAll(path).ToArray();
    if (records.Length == 0)
    {
        Console.Error.WriteLine($"No records found in {Path.GetFullPath(path)}.");
        Console.Error.WriteLine("Run samples/Runesvaner.Sample.Console first, or pass --path <directory>.");
        return 3;
    }

    Console.WriteLine($"Runesvaner report: {records.Length} request(s)");
    Console.WriteLine($"Data: {Path.GetFullPath(path)}");
    Console.WriteLine();
    Console.WriteLine("Started (UTC)        Operation  Model           Duration     TTFT  Cost             Status");

    foreach (var record in records.OrderBy(record => record.StartedAtUtc))
    {
        var duration = (record.DurationUs / 1000.0).ToString("F1", CultureInfo.InvariantCulture);
        var ttft = record.TtftUs is null
            ? "-"
            : $"{(record.TtftUs.Value / 1000.0).ToString("F1", CultureInfo.InvariantCulture)} ms";

        Console.WriteLine(
            $"{record.StartedAtUtc:yyyy-MM-dd HH:mm:ss}  {record.Operation,-9}  {record.RequestModel ?? record.ResponseModel ?? "-",-14}  {duration,8} ms  {ttft,10}  {FormatCost(record.Cost),-16}  {record.Status}");
    }

    var totalUs = records.Sum(record => record.DurationUs);
    var averageUs = totalUs / records.Length;
    Console.WriteLine();
    Console.WriteLine($"Total: {totalUs / 1000.0:F1} ms; Average: {averageUs / 1000.0:F1} ms");
    return 0;
}

static string FormatCost(CostBreakdown? cost)
{
    if (cost is null)
    {
        return "-";
    }

    return cost.Status switch
    {
        CostStatus.Known when cost.Total is not null => $"{cost.Total.Value.ToString("F6", CultureInfo.InvariantCulture)} {cost.Currency}",
        CostStatus.Partial => "partial",
        CostStatus.Unknown => "unknown",
        _ => "unknown"
    };
}

static int Costs(string path)
{
    var records = JsonlRunesvanerStore.ReadAll(path).ToArray();
    if (records.Length == 0)
    {
        Console.Error.WriteLine($"No records found in {Path.GetFullPath(path)}.");
        return 3;
    }

    Console.WriteLine($"Cost ledger: {records.Length} request(s)");
    Console.WriteLine($"Data: {Path.GetFullPath(path)}");
    Console.WriteLine();
    Console.WriteLine("Provider          Model            Requests  Priced  Partial  Unknown  Total");

    var groups = records
        .GroupBy(record => new
        {
            Provider = record.Provider ?? "-",
            Model = record.RequestModel ?? record.ResponseModel ?? "-"
        })
        .Select(group => new
        {
            group.Key.Provider,
            group.Key.Model,
            Requests = group.Count(),
            Priced = group.Count(record => record.Cost?.Status == CostStatus.Known),
            Partial = group.Count(record => record.Cost?.Status == CostStatus.Partial),
            Unknown = group.Count(record => record.Cost?.Status == CostStatus.Unknown),
            Total = group.Sum(record => record.Cost?.Status == CostStatus.Known ? record.Cost.Total ?? 0m : 0m)
        })
        .OrderByDescending(item => item.Total)
        .ThenBy(item => item.Provider, StringComparer.OrdinalIgnoreCase)
        .ThenBy(item => item.Model, StringComparer.OrdinalIgnoreCase);

    var grandTotal = 0m;
    var pricedCount = 0;

    foreach (var item in groups)
    {
        Console.WriteLine(
            $"{item.Provider,-16}  {item.Model,-15}  {item.Requests,8}  {item.Priced,6}  {item.Partial,7}  {item.Unknown,7}  {item.Total.ToString("F6", CultureInfo.InvariantCulture),12}");
        grandTotal += item.Total;
        pricedCount += item.Priced;
    }

    Console.WriteLine();
    Console.WriteLine($"Total priced requests: {pricedCount}; Total: {grandTotal.ToString("F6", CultureInfo.InvariantCulture)} USD");

    if (pricedCount == 0)
    {
        Console.Error.WriteLine("No priced records. Load a price table when recording to populate cost.");
    }

    return 0;
}

static int Pricing(string[] args)
{
    if (args.Length < 2 || !string.Equals(args[1], "validate", StringComparison.Ordinal))
    {
        Console.Error.WriteLine("Usage: runesvaner pricing validate --path <file>");
        return 2;
    }

    var path = ParseOption(args, "--path") ?? "pricing/example-prices.json";

    try
    {
        var table = RunesvanerPriceTableLoader.Load(path);
        Console.WriteLine($"Price table: {Path.GetFullPath(path)}");
        Console.WriteLine($"Version: {table.Version}");
        Console.WriteLine($"Currency: {table.Currency}");
        Console.WriteLine($"Entries: {table.Entries.Count}");
        Console.WriteLine($"Source: {table.Source ?? "-"}");
        Console.WriteLine($"Retrieved: {table.RetrievedAt?.ToString("O", CultureInfo.InvariantCulture) ?? "-"}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Invalid price table: {ex.Message}");
        return 2;
    }
}

static string? ParseOption(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.Ordinal))
        {
            return args[i + 1];
        }
    }

    return null;
}

static int Doctor(string path)
{
    var fullPath = Path.GetFullPath(path);
    var count = File.Exists(fullPath) ? 0 : JsonlRunesvanerStore.ReadAll(path).Count();

    Console.WriteLine("Runesvaner doctor");
    Console.WriteLine($"Runtime: {Environment.Version}");
    Console.WriteLine($"OS: {Environment.OSVersion}");
    Console.WriteLine($"Data path: {fullPath}");
    Console.WriteLine($"Sessions directory exists: {Directory.Exists(Path.Combine(fullPath, "sessions"))}");
    Console.WriteLine($"Records: {count}");
    return 0;
}

static int RunNotImplemented()
{
    Console.Error.WriteLine("The 'run' command is not implemented in the M1 skeleton yet.");
    Console.Error.WriteLine("Use RunesvanerChatClient in your application, then run 'runesvaner report'.");
    return 3;
}

static int Help()
{
    Console.WriteLine("runesvaner <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  report   Print a summary of captured requests (default).");
    Console.WriteLine("  doctor   Check the local data directory and record count.");
    Console.WriteLine("  costs    Aggregate captured cost by provider and model.");
    Console.WriteLine("  pricing  Validate a price table. Usage: pricing validate --path <file>.");
    Console.WriteLine("  run      Reserved for launching an application.");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --path <directory>   Data directory. Default: .runesvaner");
    return 0;
}

static int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    Console.Error.WriteLine("Run 'runesvaner --help'.");
    return 2;
}