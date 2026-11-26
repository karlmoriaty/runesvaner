using System.Text.Json.Serialization;

namespace Runesvaner;

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
    CostBreakdown? Cost = null);

[JsonSourceGenerationOptions(WriteIndented = false, UseStringEnumConverter = true)]
[JsonSerializable(typeof(RunesvanerRequestRecord))]
[JsonSerializable(typeof(CostBreakdown))]
public partial class RunesvanerJsonContext : JsonSerializerContext
{
}