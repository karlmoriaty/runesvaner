using System.Text.Json.Serialization;

namespace Runesvaner;

/// <summary>Persisted record for one logical AI request.</summary>
public sealed record RunesvanerRequestRecord(
    string Schema,
    string RequestId,
    string Operation,
    string? Provider,
    string? RequestModel,
    string? ResponseModel,
    DateTimeOffset StartedAtUtc,
    long DurationUs,
    long? TtftUs,
    string Status,
    RunesvanerUsage? Usage,
    RunesvanerSegment[] Segments,
    Dictionary<string, string>? Tags,
    CostBreakdown? Cost = null,
    RunesvanerStreamStats? Stream = null,
    string? SessionId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    RunesvanerContentCapture? Content = null,
    int Attempt = 1,
    bool PossibleDuplicateBilling = false,
    double SamplingWeight = 1);

/// <summary>Source-generated JSON context for request records and cost breakdowns.</summary>
[JsonSourceGenerationOptions(WriteIndented = false, UseStringEnumConverter = true)]
[JsonSerializable(typeof(RunesvanerRequestRecord))]
[JsonSerializable(typeof(RunesvanerRequestRecord[]))]
[JsonSerializable(typeof(CostBreakdown))]
[JsonSerializable(typeof(RunesvanerStreamStats))]
public partial class RunesvanerJsonContext : JsonSerializerContext
{
}