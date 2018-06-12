using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace BenchmarkDotNet.Samples
{
    [SimpleJob(RunStrategy.Monitoring, launchCount: 5, warmupCount: 3, targetCount: 100, id: "Monitoring")]
    [RPlotExporter]
    public class IntroIO
    {
        private byte[] data;

        /// <summary>
        /// File size in MB
        /// </summary>
        [Params(16, 32, 64)]
        public int Size;

        [GlobalSetup]
        public void GlobalSetup()
        {
            data = new byte[Size * 1024 * 1024];
        }

        [Benchmark]
        public void CreateWriteDelete()
        {
            string fileName = Path.GetTempFileName();
            File.WriteAllBytes(fileName, data);
            File.Delete(fileName);
        }
    }
}