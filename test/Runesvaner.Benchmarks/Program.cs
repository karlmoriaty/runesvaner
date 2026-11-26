using BenchmarkDotNet.Running;
using Runesvaner.Benchmarks;

if (args.Contains("--p95", StringComparer.Ordinal))
{
    P95Runner.Run();
    return;
}

BenchmarkRunner.Run<RecorderBenchmarks>();