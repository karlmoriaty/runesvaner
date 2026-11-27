using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Runesvaner;
using Runesvaner.MicrosoftExtensionsAI;
using Runesvaner.Storage.Jsonl;
using Runesvaner.Storage.Sqlite;

namespace Runesvaner.UnitTests;

public sealed class AddRunesvanerTests
{
    [Fact]
    public void AddRunesvaner_Jsonl_RegistersBackgroundStoreRecorderAndQueryStore()
    {
        var directory = CreateDirectory();

        try
        {
            using var provider = new ServiceCollection()
                .AddRunesvaner(options =>
                {
                    options.StorageMode = StorageMode.Jsonl;
                    options.StoragePath = directory;
                })
                .BuildServiceProvider();

            Assert.IsType<RunesvanerBackgroundStore>(provider.GetRequiredService<IRunesvanerStore>());
            Assert.IsType<JsonlRunesvanerQueryStore>(provider.GetRequiredService<IRunesvanerQueryStore>());
            Assert.Same(
                provider.GetRequiredService<IRunesvanerRecorder>(),
                provider.GetRequiredService<IRunesvanerRecorder>());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AddRunesvaner_CanDisableBackgroundWriter()
    {
        var directory = CreateDirectory();

        try
        {
            using var provider = new ServiceCollection()
                .AddRunesvaner(options =>
                {
                    options.EnableBackgroundWriter = false;
                    options.StorageMode = StorageMode.Jsonl;
                    options.StoragePath = directory;
                })
                .BuildServiceProvider();

            Assert.IsType<JsonlRunesvanerStore>(provider.GetRequiredService<IRunesvanerStore>());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AddRunesvaner_Sqlite_KeepsQueryStoreOnRawConnection()
    {
        var directory = CreateDirectory();
        var provider = new ServiceCollection()
            .AddRunesvaner(options =>
            {
                options.StorageMode = StorageMode.Sqlite;
                options.StoragePath = directory;
            })
            .BuildServiceProvider();

        try
        {
            Assert.IsType<RunesvanerBackgroundStore>(provider.GetRequiredService<IRunesvanerStore>());
            Assert.IsType<SqliteRunesvanerStore>(provider.GetRequiredService<IRunesvanerQueryStore>());
            Assert.True(File.Exists(Path.Combine(directory, RunesvanerServiceCollectionExtensions.SqliteFileName)));
        }
        finally
        {
            provider.Dispose();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task AddRunesvaner_Memory_ProvidesWorkingRecorderAndQueryStore()
    {
        using var provider = new ServiceCollection()
            .AddRunesvaner(options => options.StorageMode = StorageMode.Memory)
            .BuildServiceProvider();

        var recorder = provider.GetRequiredService<IRunesvanerRecorder>();

        using (var request = recorder.StartRequest(new RunesvanerRequestInfo(
                   RunesvanerOperation.Chat,
                   "test",
                   "echo-1",
                   null)))
        {
            request.Complete(new RunesvanerRequestResult(RequestStatus.Ok, 100, null));
        }

        await recorder.FlushAsync(TestContext.Current.CancellationToken);

        var records = await provider
            .GetRequiredService<IRunesvanerQueryStore>()
            .ReadRequestsAsync(null, 10, TestContext.Current.CancellationToken);

        Assert.Single(records);
    }

    [Fact]
    public async Task UseRunesvaner_ResolvesRecorderFromServices()
    {
        var directory = CreateDirectory();
        var provider = new ServiceCollection()
            .AddRunesvaner(options =>
            {
                options.ApplicationName = "add-runesvaner-test";
                options.Provider = "test";
                options.StorageMode = StorageMode.Jsonl;
                options.StoragePath = directory;
            })
            .BuildServiceProvider();

        try
        {
            var client = new ChatClientBuilder(new EchoChatClient())
                .UseRunesvaner()
                .Build(provider);

            var response = await client.GetResponseAsync(
                [new ChatMessage(ChatRole.User, "hello")],
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("echo", response.Text);

            await provider
                .GetRequiredService<IRunesvanerRecorder>()
                .FlushAsync(TestContext.Current.CancellationToken);

            Assert.Single(JsonlRunesvanerStore.ReadAll(directory));
            Assert.Equal(
                "add-runesvaner-test",
                provider.GetRequiredService<RunesvanerOptions>().ApplicationName);
        }
        finally
        {
            provider.Dispose();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AddRunesvaner_RejectsMissingStoragePath()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddRunesvaner(options =>
            {
                options.StorageMode = StorageMode.Jsonl;
                options.StoragePath = "  ";
            }));
    }

    private static string CreateDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "runesvaner-add-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class EchoChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "echo"))
            {
                ModelId = "echo-1",
                Usage = new UsageDetails { InputTokenCount = 1, OutputTokenCount = 1 }
            });

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}