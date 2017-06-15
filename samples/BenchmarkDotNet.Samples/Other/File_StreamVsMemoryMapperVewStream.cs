using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Other
{
    /// <summary>
    /// Example of reading all lines from file to string[], and usage of the [GlobalSetup] and [GlobalCleanup] attributes.
    /// </summary>
    [Config(typeof(Config))]
    public class File_StreamVsMemoryMapperVewStream
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(new Job
                {
                    Run = { LaunchCount = 2, TargetCount = 10 }
                });
            }
        }

        private int MaxLines = 1000;
        private int MaxGuidsInLine = 250;

        private string fileName;

        [GlobalSetup]
        public void GlobalSetup()
        {
            // Recreate file each time for the purpose of minimizing OS IO caching.
            fileName = Guid.NewGuid().ToString() + ".tmp";

            string g = string.Join("", Enumerable.Repeat(new Guid().ToString(), MaxGuidsInLine));

            // Write some data, don't care about performance.
            using (var sw = File.CreateText(fileName))
            {
                for (int x = 0; x < MaxLines; x++)
                {
                    sw.WriteLine(g);
                }
            }
        }

        [Benchmark(Baseline = true)]
        public void StandartRead()
        {
            using (Stream stream = File.OpenRead(fileName))
            {
                Read(stream);
            }
        }

        [Benchmark]
        public void MemoryMappedView()
        {
            using (MemoryMappedFile file = MemoryMappedFile.CreateFromFile(fileName))
            using (MemoryMappedViewStream stream = file.CreateViewStream())
            {
                Read(stream);
            }
        }

        private void Read(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string u = reader.ReadLine();
                }
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            File.Delete(fileName);
        }
    }
}
