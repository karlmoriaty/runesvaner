using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Runesvaner;
using Runesvaner.Storage.Jsonl;
using Runesvaner.Storage.Sqlite;

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

var databasePath = Path.Combine(dataPath, "runesvaner.db");
IRunesvanerQueryStore queryStore;

if (File.Exists(databasePath))
{
    try
    {
        queryStore = new SqliteRunesvanerStore(databasePath);
    }
    catch (Exception)
    {
        queryStore = new JsonlRunesvanerQueryStore(dataPath);
    }
}
else
{
    queryStore = new JsonlRunesvanerQueryStore(dataPath);
}

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
    dataPath = Path.GetFullPath(dataPath),
    storage = queryStore.GetType().Name
}));

app.MapGet("/api/costs", async (CancellationToken cancellationToken) =>
{
    var records = await queryStore.ReadRequestsAsync(null, int.MaxValue, cancellationToken);
    return Results.Ok(RunesvanerCostAggregator.Aggregate(records));
});

app.MapGet("/api/sessions", async (CancellationToken cancellationToken) =>
    Results.Ok(await queryStore.ReadSessionsAsync(cancellationToken)));

app.MapGet("/api/requests", async (int? limit, string? session, CancellationToken cancellationToken) =>
{
    var take = Math.Clamp(limit ?? 100, 1, 500);
    var records = await queryStore.ReadRequestsAsync(session, take, cancellationToken);

    var summaries = records.Select(record => new
    {
        record.RequestId,
        record.SessionId,
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

    return Results.Ok(summaries);
});

app.MapGet("/api/requests/{requestId}", async (string requestId, CancellationToken cancellationToken) =>
{
    var record = await queryStore.GetRequestAsync(requestId, cancellationToken);
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