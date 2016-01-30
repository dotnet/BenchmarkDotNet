using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.CPU
{
    [Config(typeof(Config))]
    public class Cpu_Atomics
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.RyuJitX64);
            }
        }

        private int a;
        private object syncRoot = new object();

        [Benchmark(OperationsPerInvoke = 4)]
        public void Lock()
        {
            lock (syncRoot) a++;
            lock (syncRoot) a++;
            lock (syncRoot) a++;
            lock (syncRoot) a++;
        }

        [Benchmark(OperationsPerInvoke = 4)]
        public void Interlocked()
        {
            System.Threading.Interlocked.Increment(ref a);
            System.Threading.Interlocked.Increment(ref a);
            System.Threading.Interlocked.Increment(ref a);
            System.Threading.Interlocked.Increment(ref a);
        }

        [Benchmark(OperationsPerInvoke = 4)]
        public void NoLock()
        {
            a++;
            a++;
            a++;
            a++;
        }
    }
}
