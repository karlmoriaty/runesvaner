using Microsoft.Extensions.DependencyInjection;
using Runesvaner.Storage.Jsonl;
using Runesvaner.Storage.Sqlite;

namespace Runesvaner;

/// <summary>Dependency injection registration for Runesvaner.</summary>
public static class RunesvanerServiceCollectionExtensions
{
    /// <summary>The SQLite database file name under StoragePath.</summary>
    public const string SqliteFileName = "runesvaner.db";

    /// <summary>Registers options, the selected store, a recorder, and a query store.</summary>
    public static IServiceCollection AddRunesvaner(
        this IServiceCollection services,
        Action<RunesvanerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new RunesvanerOptions();
        configure?.Invoke(options);
        Validate(options);

        services.AddSingleton(options);
        services.AddSingleton(provider => new RunesvanerStoreHolder(CreateStore(options)));
        services.AddSingleton<RunesvanerSelfMonitor>();
        services.AddSingleton(provider => new RunesvanerBudgetTracker(options.Budget));
        services.AddSingleton<IRunesvanerStore>(provider =>
        {
            var raw = provider.GetRequiredService<RunesvanerStoreHolder>().Store;

            return options.EnableBackgroundWriter
                ? new RunesvanerBackgroundStore(
                    raw,
                    options,
                    provider.GetRequiredService<RunesvanerSelfMonitor>())
                : raw;
        });
        services.AddSingleton<IRunesvanerRecorder>(provider =>
            new RunesvanerRecorder(
                provider.GetRequiredService<IRunesvanerStore>(),
                options,
                budget: provider.GetRequiredService<RunesvanerBudgetTracker>()));

        if (options.StorageMode == StorageMode.Jsonl)
        {
            services.AddSingleton<IRunesvanerQueryStore>(
                _ => new JsonlRunesvanerQueryStore(options.StoragePath));
        }
        else
        {
            services.AddSingleton<IRunesvanerQueryStore>(provider =>
                provider.GetRequiredService<RunesvanerStoreHolder>().Store as IRunesvanerQueryStore
                ?? throw new InvalidOperationException(
                    $"Storage mode '{options.StorageMode}' does not provide a query store."));
        }

        return services;
    }

    private static IRunesvanerStore CreateStore(RunesvanerOptions options)
        => options.StorageMode switch
        {
            StorageMode.Jsonl => new JsonlRunesvanerStore(options.StoragePath, null, options),
            StorageMode.Sqlite => new SqliteRunesvanerStore(
                Path.Combine(options.StoragePath, SqliteFileName),
                options),
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

        if (options.BackgroundQueueCapacity < 1)
        {
            throw new InvalidOperationException(
                "RunesvanerOptions.BackgroundQueueCapacity must be at least 1.");
        }
    }
}

internal sealed class RunesvanerStoreHolder(IRunesvanerStore store) : IDisposable
{
    public IRunesvanerStore Store { get; } = store;

    public void Dispose() => (Store as IDisposable)?.Dispose();
}