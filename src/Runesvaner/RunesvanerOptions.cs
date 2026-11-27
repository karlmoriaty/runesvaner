namespace Runesvaner;

public enum StorageMode
{
    Jsonl,
    Sqlite,
    Memory
}

public sealed class RunesvanerOptions
{
    public bool Enabled { get; set; } = true;

    public string ApplicationName { get; set; } = "";

    public string Environment { get; set; } = "";

    public string Provider { get; set; } = "";

    public string? SessionId { get; set; }

    public PriceTable? PricingTable { get; set; } = RunesvanerPriceTableLoader.LoadBuiltIn();

    public StorageMode StorageMode { get; set; } = StorageMode.Sqlite;

    public string StoragePath { get; set; } = ".runesvaner";

    public bool EnableBackgroundWriter { get; set; } = true;

    public int BackgroundQueueCapacity { get; set; } = 4_096;

    public ContentCaptureMode ContentCapture { get; set; } = ContentCaptureMode.None;

    public int RetentionDays { get; set; }

    public long MaxStorageBytes { get; set; }
}