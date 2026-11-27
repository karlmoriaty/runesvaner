using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Runesvaner;
using Runesvaner.Storage.Jsonl;

var command = args.Length > 0 ? args[0] : "report";

return command switch
{
    "report" => Report(ParsePath(args)),
    "doctor" => Doctor(ParsePath(args)),
    "costs" => Costs(ParsePath(args)),
    "pricing" => Pricing(args),
    "run" => Run(args),
    "export" => Export(args),
    "open" => Open(args),
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

        if (record.Stream is { } stream)
        {
            Console.WriteLine(FormatStream(stream));
        }
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

    var summaries = RunesvanerCostAggregator.Aggregate(records);
    var grandTotal = summaries.Sum(summary => summary.Total);
    var pricedCount = summaries.Sum(summary => summary.Priced);

    foreach (var item in summaries)
    {
        Console.WriteLine(
            $"{item.Provider,-16}  {item.Model,-15}  {item.Requests,8}  {item.Priced,6}  {item.Partial,7}  {item.Unknown,7}  {item.Total.ToString("F6", CultureInfo.InvariantCulture),12}");
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
        Console.Error.WriteLine("Usage: runesvaner pricing validate [--path <file> | --builtin]");
        return 2;
    }

    var builtIn = Array.Exists(args, arg => string.Equals(arg, "--builtin", StringComparison.Ordinal));
    var path = ParseOption(args, "--path");

    if (builtIn && path is not null)
    {
        Console.Error.WriteLine("Use either --path or --builtin, not both.");
        return 2;
    }

    var location = builtIn
        ? "builtin"
        : Path.GetFullPath(path ?? "pricing/example-prices.json");

    try
    {
        var table = builtIn
            ? RunesvanerPriceTableLoader.LoadBuiltIn()
            : RunesvanerPriceTableLoader.Load(location);
        Console.WriteLine($"Price table: {location}");
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

static string FormatStream(RunesvanerStreamStats stream)
{
    var itl = stream.ItlP95Us is null
        ? "-"
        : $"{(stream.ItlP95Us.Value / 1000.0).ToString("F1", CultureInfo.InvariantCulture)} ms";
    var tps = stream.TokensPerSecond is null
        ? "-"
        : stream.TokensPerSecond.Value.ToString("F1", CultureInfo.InvariantCulture);

    var flags = new List<string>();
    if (stream.Cancelled)
    {
        flags.Add("cancelled");
    }

    if (stream.Interrupted)
    {
        flags.Add("interrupted");
    }

    var suffix = flags.Count == 0 ? "" : $" {string.Join(' ', flags)}";
    return $"    stream chunks={stream.ChunkCount} itl_p95={itl} tps={tps} itl_estimated={stream.ItlEstimated} finish={stream.FinishReason ?? "-"}{suffix}";
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

static int Run(string[] args)
{
    var separator = Array.IndexOf(args, "--");

    if (separator < 0 || separator == args.Length - 1)
    {
        Console.Error.WriteLine("Usage: runesvaner run [--path <directory>] -- <command> [args...]");
        return 2;
    }

    var dataPath = Path.GetFullPath(ParsePath(args));
    var executable = args[separator + 1];

    var startInfo = new ProcessStartInfo(executable)
    {
        UseShellExecute = false
    };

    for (var i = separator + 2; i < args.Length; i++)
    {
        startInfo.ArgumentList.Add(args[i]);
    }

    startInfo.Environment["RUNESVANER_DATA_PATH"] = dataPath;

    try
    {
        using var process = Process.Start(startInfo);

        if (process is null)
        {
            Console.Error.WriteLine($"Failed to start '{executable}'.");
            return 3;
        }

        process.WaitForExit();
        return process.ExitCode;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to start '{executable}': {ex.Message}");
        return 3;
    }
}

static int Export(string[] args)
{
    var dataPath = ParsePath(args);
    var format = ParseOption(args, "--format") ?? "json";
    var output = Path.GetFullPath(ParseOption(args, "--output") ?? "runesvaner-export.json");

    var records = JsonlRunesvanerStore.ReadAll(dataPath).ToArray();

    if (records.Length == 0)
    {
        Console.Error.WriteLine($"No records found in {Path.GetFullPath(dataPath)}.");
        return 3;
    }

    var directory = Path.GetDirectoryName(output);
    if (!string.IsNullOrEmpty(directory))
    {
        Directory.CreateDirectory(directory);
    }

    if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
    {
        File.WriteAllText(
            output,
            JsonSerializer.Serialize(records, RunesvanerJsonContext.Default.RunesvanerRequestRecordArray));
    }
    else if (string.Equals(format, "jsonl", StringComparison.OrdinalIgnoreCase))
    {
        using var writer = new StreamWriter(output);

        foreach (var record in records)
        {
            writer.WriteLine(JsonSerializer.Serialize(
                record,
                RunesvanerJsonContext.Default.RunesvanerRequestRecord));
        }
    }
    else
    {
        Console.Error.WriteLine($"Unsupported format '{format}'. Use json or jsonl.");
        return 2;
    }

    Console.WriteLine($"Exported {records.Length} request(s) to {output} ({format}).");
    return 0;
}

static int Open(string[] args)
{
    var dataPath = Path.GetFullPath(ParsePath(args));
    var studioAssembly = Path.Combine(AppContext.BaseDirectory, "Runesvaner.Studio.dll");

    if (!File.Exists(studioAssembly))
    {
        Console.Error.WriteLine("Runesvaner.Studio.dll was not found next to the CLI.");
        Console.Error.WriteLine("Run it from source instead: dotnet run --project src/Runesvaner.Studio -c Release");
        return 3;
    }

    var startInfo = new ProcessStartInfo("dotnet")
    {
        UseShellExecute = false
    };

    startInfo.ArgumentList.Add(studioAssembly);
    startInfo.ArgumentList.Add($"--Studio:DataPath={dataPath}");

    try
    {
        using var process = Process.Start(startInfo);

        if (process is null)
        {
            Console.Error.WriteLine("Failed to start Runesvaner Studio.");
            return 3;
        }

        Console.WriteLine($"Started Runesvaner Studio for {dataPath}.");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to start Runesvaner Studio: {ex.Message}");
        return 3;
    }
}

static int Help()
{
    Console.WriteLine("runesvaner <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  report   Print a summary of captured requests (default).");
    Console.WriteLine("  doctor   Check the local data directory and record count.");
    Console.WriteLine("  costs    Aggregate captured cost by provider and model.");
    Console.WriteLine("  pricing  Validate a price table. Usage: pricing validate [--path <file> | --builtin].");
    Console.WriteLine("  run      Run a command with RUNESVANER_DATA_PATH configured.");
    Console.WriteLine("  export   Export captured requests (--format json|jsonl, --output <file>).");
    Console.WriteLine("  open     Start the local Studio.");
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