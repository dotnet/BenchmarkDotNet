using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Toolchains;

public interface IBuilder
{
    ValueTask<BuildResult> BuildAsync(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger, CancellationToken cancellationToken);
}