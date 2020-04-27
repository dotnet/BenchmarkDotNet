using System;
using System.IO;
using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests.CustomPaths
{
    public class BenchmarksThatUsesFileFromOutput
    {
        [Benchmark]
        public void Verify()
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "ShouldGetCopied.xml")))
            {
                throw new InvalidOperationException("the file did not get copied");
            }
        }
    }
}