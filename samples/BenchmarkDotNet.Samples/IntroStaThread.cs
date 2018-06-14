using System.Threading;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    public class IntroStaThread
    {
        [Benchmark, System.STAThread]
        public void CheckForSTA()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new ThreadStateException(
                    "The current threads apartment state is not STA");
            }
        }
    }
}