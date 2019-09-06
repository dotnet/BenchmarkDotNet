using BenchmarkDotNet.Attributes;
using System.Threading;

namespace BenchmarkDotNet.Samples
{
    [ThreadingDiagnoser] // ENABLE the diagnoser
    public class IntroThreadingDiagnoser
    {
        [Benchmark]
        public void CompleteOneWorkItem()
        {
            ManualResetEvent done = new ManualResetEvent(initialState: false);

            ThreadPool.QueueUserWorkItem(m => (m as ManualResetEvent).Set(), done);

            done.WaitOne();
        }
    }
}
