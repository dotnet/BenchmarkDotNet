using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples.CPU
{
    [BenchmarkTask(platform: BenchmarkPlatform.X64, jitVersion: BenchmarkJitVersion.RyuJit)]
    public class Cpu_Atomics
    {
        private int a;
        private object syncRoot = new object();

        [Benchmark]
        [OperationsPerInvoke(4)]
        public void Lock()
        {
            lock (syncRoot) a++;
            lock (syncRoot) a++;
            lock (syncRoot) a++;
            lock (syncRoot) a++;
        }

        [Benchmark]
        [OperationsPerInvoke(4)]
        public void Interlocked()
        {
            System.Threading.Interlocked.Increment(ref a);
            System.Threading.Interlocked.Increment(ref a);
            System.Threading.Interlocked.Increment(ref a);
            System.Threading.Interlocked.Increment(ref a);
        }

        [Benchmark]
        [OperationsPerInvoke(4)]
        public void NoLock()
        {
            a++;
            a++;
            a++;
            a++;
        }
    }
}
