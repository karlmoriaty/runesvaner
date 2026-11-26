using System.Globalization;
using Runesvaner;
using Runesvaner.Storage.Jsonl;

var command = args.Length > 0 ? args[0] : "report";

return command switch
{
    "report" => Report(ParsePath(args)),
    "doctor" => Doctor(ParsePath(args)),
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
    Console.WriteLine("  run      Reserved for launching an application (M1.1).");
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