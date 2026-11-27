using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Runesvaner;
using Runesvaner.Storage.Jsonl;

namespace Runesvaner.IntegrationTests;

public sealed class StudioApiTests
{
    [Fact]
    public async Task Costs_RequiresTokenAndReturnsSummary()
    {
        var dataPath = Path.Combine(
            Path.GetTempPath(),
            "runesvaner-studio-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataPath);

        try
        {
            var store = new JsonlRunesvanerStore(dataPath);
            await store.WriteAsync(Record(), TestContext.Current.CancellationToken);

            using var factory = new StudioFactory(dataPath);
            using var anonymous = factory.CreateClient();

            var unauthorized = await anonymous.GetAsync(
                "/api/costs",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "test-token");

            var response = await client.GetAsync(
                "/api/costs",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var costs = await response.Content.ReadFromJsonAsync<List<CostRow>>(
                TestContext.Current.CancellationToken);
            Assert.NotNull(costs);

            var row = Assert.Single(costs!);
            Assert.Equal("sample-rag", row.Provider);
            Assert.Equal("echo-1", row.Model);
            Assert.Equal(1, row.Priced);
            Assert.Equal(0.000009m, row.Total);

            var detailResponse = await client.GetAsync(
                "/api/requests/studio-1",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

            var detailJson = await detailResponse.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);
            Assert.Contains("\"requestId\":\"studio-1\"", detailJson);
            Assert.Contains("\"segments\":", detailJson);
            Assert.Contains("\"Embedding\"", detailJson);

            var missing = await client.GetAsync(
                "/api/requests/not-found",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

            var sessionsResponse = await client.GetAsync(
                "/api/sessions",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, sessionsResponse.StatusCode);
            var sessionsJson = await sessionsResponse.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);
            Assert.Contains("studio-session", sessionsJson);
            Assert.Contains("\"totalCost\":0.000009", sessionsJson);

            var filtered = await client.GetAsync(
                "/api/requests?session=studio-session",
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
            var filteredJson = await filtered.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);
            Assert.Contains("\"sessionId\":\"studio-session\"", filteredJson);

            using var badHostRequest = new HttpRequestMessage(HttpMethod.Get, "/");
            badHostRequest.Headers.Host = "evil.example";
            var badHost = await anonymous.SendAsync(
                badHostRequest,
                TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, badHost.StatusCode);
        }
        finally
        {
            Directory.Delete(dataPath, recursive: true);
        }
    }

    private sealed class StudioFactory : WebApplicationFactory<Program>
    {
        private readonly string _dataPath;

        public StudioFactory(string dataPath) => _dataPath = dataPath;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Studio:Token", "test-token");
            builder.UseSetting("Studio:DataPath", _dataPath);
        }
    }

    private sealed record CostRow(
        string Provider,
        string Model,
        int Requests,
        int Priced,
        int Partial,
        int Unknown,
        decimal Total);

    private static RunesvanerRequestRecord Record()
        => new(
            "runesvaner.request.v1",
            "studio-1",
            "Rag",
            "sample-rag",
            "echo-1",
            null,
            DateTimeOffset.UtcNow,
            1_000,
            null,
            "Ok",
            new RunesvanerUsage(128, 24, 96, null, UsageSource.Estimated),
            new[]
            {
                new RunesvanerSegment(SegmentKind.Embedding, 0, 400, false, SegmentMeasurement.Stopwatch),
                new RunesvanerSegment(SegmentKind.VectorSearch, 400, 200, false, SegmentMeasurement.Stopwatch),
                new RunesvanerSegment(SegmentKind.Generation, 600, 400, false, SegmentMeasurement.Stopwatch)
            },
            new Dictionary<string, string> { ["feature"] = "rag.sample" },
            new CostBreakdown("USD", 0.0000032m, 0.00000096m, 0.0000048m, 0m, 0.000009m, CostStatus.Known, "test-1"),
            SessionId: "studio-session");
}