﻿using BenchmarkDotNet.Helpers;
using Xunit;

namespace BenchmarkDotNet.Tests.Portability
{
    public class WmicCpuInfoParserTests
    {
        
        [Fact]
        public void EmptyTest()
        {
            var parser = new WmicCpuInfoParser(string.Empty);
            Assert.Equal(null, parser.PhysicalProcessorCount);
            Assert.Equal(null, parser.PhysicalCoreCount);
            Assert.Equal(null, parser.LogicalCoreCount);
            Assert.Equal(null, parser.ProcessorName);
        }

        [Fact]
        public void MalformedTest()
        {
            var parser = new WmicCpuInfoParser("malformedkey=malformedvalue\n\nmalformedkey2=malformedvalue2");
            Assert.Equal(null, parser.PhysicalProcessorCount);
            Assert.Equal(null, parser.PhysicalCoreCount);
            Assert.Equal(null, parser.LogicalCoreCount);
            Assert.Equal(null, parser.ProcessorName);
        }
        
        [Fact]
        public void RealTwoProcessorEightCoresTest()
        {
            #region cpuinfo

            var cpuInfo = @"

Name=Intel(R) Xeon(R) CPU E5-2630 v3 @ 2.40GHz
NumberOfCores=8
NumberOfLogicalProcessors=16


Name=Intel(R) Xeon(R) CPU E5-2630 v3 @ 2.40GHz
NumberOfCores=8
NumberOfLogicalProcessors=16

";

            #endregion

            var parser = new WmicCpuInfoParser(cpuInfo);
            Assert.Equal(2, parser.PhysicalProcessorCount);
            Assert.Equal(16, parser.PhysicalCoreCount);
            Assert.Equal(32, parser.LogicalCoreCount);
            Assert.Equal("Intel(R) Xeon(R) CPU E5-2630 v3 @ 2.40GHz", parser.ProcessorName);
        }
        
        [Fact]
        public void RealOneProcessorFourCoresTest()
        {
            #region cpuinfo

            var cpuInfo = @"


Name=Intel(R) Core(TM) i7-4710MQ CPU @ 2.50GHz
NumberOfCores=4
NumberOfLogicalProcessors=8

";

            #endregion

            var parser = new WmicCpuInfoParser(cpuInfo);
            Assert.Equal(1, parser.PhysicalProcessorCount);
            Assert.Equal(4, parser.PhysicalCoreCount);
            Assert.Equal(8, parser.LogicalCoreCount);
            Assert.Equal("Intel(R) Core(TM) i7-4710MQ CPU @ 2.50GHz", parser.ProcessorName);
        }
    }
}