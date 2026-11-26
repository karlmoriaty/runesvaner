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

    public PriceTable? PricingTable { get; set; }

    public StorageMode StorageMode { get; set; } = StorageMode.Sqlite;

    public string StoragePath { get; set; } = ".runesvaner";
}