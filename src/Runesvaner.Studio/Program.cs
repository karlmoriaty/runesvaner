using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Runesvaner;
using Runesvaner.Storage.Jsonl;

var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
Directory.CreateDirectory(webRoot);

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = webRoot
});

builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

var token = builder.Configuration["Studio:Token"]
    ?? Environment.GetEnvironmentVariable("RUNESVANER_STUDIO_TOKEN")
    ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

var dataPath = builder.Configuration["Studio:DataPath"]
    ?? Environment.GetEnvironmentVariable("RUNESVANER_DATA_PATH")
    ?? FindDataDirectory();

app.Use(async (context, next) =>
{
    var host = context.Request.Host.Host;
    if (!string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Invalid Host header.");
        return;
    }

    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (!string.Equals(authorization, $"Bearer {token}", StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
    }

    await next();
});

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    version = "0.1.0",
    dataPath = Path.GetFullPath(dataPath)
}));

app.MapGet("/api/costs", () =>
{
    var records = JsonlRunesvanerStore.ReadAll(dataPath);
    return Results.Ok(RunesvanerCostAggregator.Aggregate(records));
});

app.MapGet("/api/requests", (int? limit) =>
{
    var take = Math.Clamp(limit ?? 100, 1, 500);
    var records = JsonlRunesvanerStore.ReadAll(dataPath)
        .OrderByDescending(record => record.StartedAtUtc)
        .Take(take)
        .Select(record => new
        {
            record.RequestId,
            record.Operation,
            record.Provider,
            Model = record.RequestModel ?? record.ResponseModel ?? "-",
            record.StartedAtUtc,
            record.DurationUs,
            record.TtftUs,
            record.Status,
            Cost = record.Cost?.Total,
            CostStatus = record.Cost?.Status.ToString() ?? "None"
        });

    return Results.Ok(records);
});

app.MapGet("/api/requests/{requestId}", (string requestId) =>
{
    var record = JsonlRunesvanerStore.ReadAll(dataPath)
        .FirstOrDefault(candidate => string.Equals(candidate.RequestId, requestId, StringComparison.Ordinal));

    return record is null ? Results.NotFound() : Results.Ok(record);
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Lifetime.ApplicationStarted.Register(() =>
{
    var url = app.Urls.FirstOrDefault() ?? "http://127.0.0.1";
    Console.WriteLine($"Runesvaner Studio: {url}/?token={token}");
});

app.Run();

static string FindDataDirectory()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, ".runesvaner");
        if (Directory.Exists(candidate))
        {
            return candidate;
        }

        directory = directory.Parent;
    }

    return Path.Combine(Directory.GetCurrentDirectory(), ".runesvaner");
}

public partial class Program;