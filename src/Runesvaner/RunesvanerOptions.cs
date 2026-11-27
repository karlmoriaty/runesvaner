namespace Runesvaner;

/// <summary>Storage backend used by the default store factory.</summary>
public enum StorageMode
{
    /// <summary>JSON Lines files under the storage path.</summary>
    Jsonl,

    /// <summary>SQLite database at storage-path/runesvaner.db.</summary>
    Sqlite,

    /// <summary>In-process memory store.</summary>
    Memory
}

/// <summary>Configuration for recording, storage, pricing, privacy, and reliability.</summary>
public sealed class RunesvanerOptions
{
    /// <summary>Gets or sets whether collection is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the application name recorded with captures.</summary>
    public string ApplicationName { get; set; } = "";

    /// <summary>Gets or sets the environment name recorded with captures.</summary>
    public string Environment { get; set; } = "";

    /// <summary>Gets or sets the default provider used for price lookup.</summary>
    public string Provider { get; set; } = "";

    /// <summary>Gets or sets a fixed session identifier for all requests.</summary>
    public string? SessionId { get; set; }

    /// <summary>Gets or sets the price table; defaults to the embedded verified snapshot.</summary>
    public PriceTable? PricingTable { get; set; } = RunesvanerPriceTableLoader.LoadBuiltIn();

    /// <summary>Gets or sets the storage backend used by AddRunesvaner().</summary>
    public StorageMode StorageMode { get; set; } = StorageMode.Sqlite;

    /// <summary>Gets or sets the data directory used by AddRunesvaner().</summary>
    public string StoragePath { get; set; } = ".runesvaner";

    /// <summary>Gets or sets whether writes are funneled through the bounded background writer.</summary>
    public bool EnableBackgroundWriter { get; set; } = true;

    /// <summary>Gets or sets the background writer queue capacity.</summary>
    public int BackgroundQueueCapacity { get; set; } = 4_096;

    /// <summary>Gets or sets how much prompt and response content is captured.</summary>
    public ContentCaptureMode ContentCapture { get; set; } = ContentCaptureMode.None;

    /// <summary>Gets or sets the retention window in days; zero disables time-based pruning.</summary>
    public int RetentionDays { get; set; }

    /// <summary>Gets or sets the maximum stored bytes; zero disables size-based pruning.</summary>
    public long MaxStorageBytes { get; set; }

    /// <summary>Gets or sets the head sampling rate between 0 and 1.</summary>
    public double SamplingRate { get; set; } = 1;

    /// <summary>Gets or sets the tail sampling policy applied after a request completes.</summary>
    public RunesvanerTailSamplingOptions TailSampling { get; set; } = new();

    /// <summary>Gets or sets the budget thresholds and alert callback.</summary>
    public RunesvanerBudgetOptions Budget { get; set; } = new();
}