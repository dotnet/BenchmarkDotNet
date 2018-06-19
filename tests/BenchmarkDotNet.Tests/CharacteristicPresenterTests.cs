using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class CharacteristicPresenterTests
    {
        [Fact]
        public void ProcessorAffinityIsPrintedAsBitMask()
        {
            var jobWithAffinity = Job.Default.WithAffinity((IntPtr) 1);

            var expected = "1".PadLeft(Environment.ProcessorCount, '0');
            
            var printed = CharacteristicPresenter.SummaryPresenter.ToPresentation(jobWithAffinity, EnvironmentMode.AffinityCharacteristic);

            Assert.Equal(expected, printed);
        }
        
        [Fact]
        public void ProcessorAffinityIsPrintedAsBitMaskEvenWhenNotSet()
        {
            var jobWithoutAffinity = Job.Default;

            var benchmarkCase = BenchmarkConverter.TypeToBenchmarks(
                typeof(CharacteristicPresenterTests), 
                DefaultConfig.Instance.With(jobWithoutAffinity)).BenchmarksCases.Single();
            
            var column = new JobCharacteristicColumn(EnvironmentMode.AffinityCharacteristic);
            
            var expected = "1".PadLeft(Environment.ProcessorCount, '1');

            var printed = column.GetValue(summary: null, benchmarkCase);

            Assert.Equal(expected, printed);
        }
        
        [Benchmark]
        public void Mehtod() { }
    }
}