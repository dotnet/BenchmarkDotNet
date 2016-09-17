using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Engines
{
    public class Consumer
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Consume<T>(T x)
        {
            // TODO
        }
    }
}