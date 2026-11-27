namespace Runesvaner;

/// <summary>Tail sampling policy applied after a request completes.</summary>
public sealed class RunesvanerTailSamplingOptions
{
    /// <summary>Gets or sets whether tail sampling is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the retention rate for successful, fast requests (0 to 1).</summary>
    public double SampleRate { get; set; } = 1;

    /// <summary>Gets or sets the duration threshold in microseconds that is always kept.</summary>
    public long SlowThresholdUs { get; set; }

    /// <summary>Gets or sets the cost threshold in USD that is always kept.</summary>
    public decimal? OverBudgetUsd { get; set; }
}