using System;
using BenchmarkDotNet.Loggers;
using Xunit;

namespace BenchmarkDotNet.Tests.Loggers
{
    public class LoggerWithPrefixTests
    {
        private readonly LoggerWithPrefix loggerWithPrefix;
        private readonly AccumulationLogger logger;

        public LoggerWithPrefixTests()
        {
            logger = new AccumulationLogger();
            loggerWithPrefix = new LoggerWithPrefix(logger, "prefix");
        }

        [Fact]
        public void Write()
        {
            loggerWithPrefix.Write("1");
            loggerWithPrefix.Write("2");
            Assert.Equal("prefix12", logger.GetLog());
        }

        [Fact]
        public void WriteLine()
        {
            loggerWithPrefix.WriteLine("1");
            loggerWithPrefix.WriteLine("2");
            Assert.Equal($"prefix1{Environment.NewLine}prefix2{Environment.NewLine}", logger.GetLog());
        }

        [Fact]
        public void Write_EmptyLine()
        {
            loggerWithPrefix.Write(string.Empty);
            loggerWithPrefix.Write(string.Empty);
            Assert.Equal(string.Empty, logger.GetLog());
        }

        [Fact]
        public void WriteLine_EmptyLine()
        {
            loggerWithPrefix.WriteLine(string.Empty);
            loggerWithPrefix.WriteLine(string.Empty);
            Assert.Equal($"{Environment.NewLine}{Environment.NewLine}", logger.GetLog());
        }

        [Fact]
        public void WriteLineWithoutArg()
        {
            loggerWithPrefix.WriteLine();
            loggerWithPrefix.WriteLine();
            Assert.Equal($"{Environment.NewLine}{Environment.NewLine}", logger.GetLog());
        }

        [Fact]
        public void Write_StringWithOneNewLine()
        {
            loggerWithPrefix.Write($"1{Environment.NewLine}2");
            Assert.Equal($"prefix1{Environment.NewLine}prefix2", logger.GetLog());
        }

        [Fact]
        public void WriteLine_StringWithNewLine()
        {
            loggerWithPrefix.WriteLine($"1{Environment.NewLine}2");
            Assert.Equal($"prefix1{Environment.NewLine}prefix2{Environment.NewLine}", logger.GetLog());
        }

        [Fact]
        public void Write_StringWithMultipleNewLine()
        {
            loggerWithPrefix.Write($"1{Environment.NewLine}2{Environment.NewLine}3");
            Assert.Equal($"prefix1{Environment.NewLine}prefix2{Environment.NewLine}prefix3", logger.GetLog());
        }

        [Fact]
        public void WriteLine_StringWithMultipleNewLine()
        {
            loggerWithPrefix.WriteLine($"1{Environment.NewLine}2{Environment.NewLine}3");
            Assert.Equal($"prefix1{Environment.NewLine}prefix2{Environment.NewLine}prefix3{Environment.NewLine}", logger.GetLog());
        }

        [Fact]
        public void Write_StringWithEmptyNewLine()
        {
            loggerWithPrefix.Write($"1{Environment.NewLine}{Environment.NewLine}2");
            Assert.Equal($"prefix1{Environment.NewLine}{Environment.NewLine}prefix2", logger.GetLog());
        }

        [Fact]
        public void WriteLine_StringWithEmptyNewLine()
        {
            loggerWithPrefix.WriteLine($"1{Environment.NewLine}{Environment.NewLine}2");
            Assert.Equal($"prefix1{Environment.NewLine}{Environment.NewLine}prefix2{Environment.NewLine}", logger.GetLog());
        }
    }
}