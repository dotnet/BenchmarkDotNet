using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

public interface IEngine
{
    ValueTask<RunResults> RunAsync();
}