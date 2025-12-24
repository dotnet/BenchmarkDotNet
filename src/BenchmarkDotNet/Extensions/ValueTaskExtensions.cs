using System.Threading.Tasks;

namespace BenchmarkDotNet.Extensions
{
    internal static class ValueTaskExtensions
    {
        public static T WaitForResult<T>(this ValueTask<T> task)
            => task.IsCompletedSuccessfully
            ? task.GetAwaiter().GetResult()
            : task.AsTask().GetAwaiter().GetResult();
    }
}
