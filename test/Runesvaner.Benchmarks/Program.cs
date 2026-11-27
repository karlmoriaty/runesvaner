using BenchmarkDotNet.Running;
using Runesvaner.Benchmarks;

if (args.Contains("--gate", StringComparer.Ordinal))
{
    return PerformanceGate.Run();
}

if (args.Contains("--p95", StringComparer.Ordinal))
{
    P95Runner.Run();
    return 0;
}

BenchmarkRunner.Run<RecorderBenchmarks>();
return 0;