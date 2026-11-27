using Microsoft.Extensions.DependencyInjection;
using Runesvaner.Storage.Jsonl;
using Runesvaner.Storage.Sqlite;

namespace Runesvaner;

public static class RunesvanerServiceCollectionExtensions
{
    public const string SqliteFileName = "runesvaner.db";

    public static IServiceCollection AddRunesvaner(
        this IServiceCollection services,
        Action<RunesvanerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new RunesvanerOptions();
        configure?.Invoke(options);
        Validate(options);

        services.AddSingleton(options);
        services.AddSingleton<IRunesvanerStore>(_ => CreateStore(options));
        services.AddSingleton<IRunesvanerRecorder>(provider =>
            new RunesvanerRecorder(
                provider.GetRequiredService<IRunesvanerStore>(),
                options));

        if (options.StorageMode == StorageMode.Jsonl)
        {
            services.AddSingleton<IRunesvanerQueryStore>(
                _ => new JsonlRunesvanerQueryStore(options.StoragePath));
        }
        else
        {
            services.AddSingleton<IRunesvanerQueryStore>(provider =>
                provider.GetRequiredService<IRunesvanerStore>() as IRunesvanerQueryStore
                ?? throw new InvalidOperationException(
                    $"Storage mode '{options.StorageMode}' does not provide a query store."));
        }

        return services;
    }

    private static IRunesvanerStore CreateStore(RunesvanerOptions options)
        => options.StorageMode switch
        {
            StorageMode.Jsonl => new JsonlRunesvanerStore(options.StoragePath),
            StorageMode.Sqlite => new SqliteRunesvanerStore(
                Path.Combine(options.StoragePath, SqliteFileName)),
            StorageMode.Memory => new InMemoryRunesvanerStore(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(options),
                options.StorageMode,
                "Unsupported storage mode.")
        };

    private static void Validate(RunesvanerOptions options)
    {
        if (options.StorageMode != StorageMode.Memory &&
            string.IsNullOrWhiteSpace(options.StoragePath))
        {
            throw new InvalidOperationException(
                "RunesvanerOptions.StoragePath is required unless StorageMode is Memory.");
        }
    }
}