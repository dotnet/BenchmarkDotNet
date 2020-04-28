using BenchmarkDotNet.Environments;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Environments
{
    public class OsBrandStringTests
    {
        private readonly ITestOutputHelper output;

        public OsBrandStringTests(ITestOutputHelper output) => this.output = output;

        private void Check(string actual, string expected)
        {
            output.WriteLine("LENGTH   : " + actual.Length);
            output.WriteLine("ACTUAL   : " + actual);
            output.WriteLine("EXPECTED : " + expected);
            Assert.Equal(expected, actual);

            // The line with OsBrandString is one of the longest lines in the summary.
            // When people past in on GitHub, it can be a reason of an ugly horizontal scrollbar.
            // To avoid this, we are trying to minimize this line and use the minimum possible number of characters.
            // In this test, we check that the length of the OS brand string for typical Windows versions
            // is less than 60 characters.
            Assert.True(actual.Length <= 60);
        }

        [Theory]
        [InlineData("6.3.9600", "Windows 8.1 (6.3.9600)")]
        [InlineData("10.0.14393", "Windows 10.0.14393 (1607/AnniversaryUpdate/Redstone1)")]
        public void WindowsIsPrettified(string originalVersion, string prettifiedName)
            => Check(OsBrandStringHelper.Prettify("Windows", originalVersion), prettifiedName);

        [Theory]
        [InlineData("10.0.10240", 17797, "Windows 10.0.10240.17797 (1507/RTM/Threshold1)")]
        [InlineData("10.0.10586", 1478, "Windows 10.0.10586.1478 (1511/NovemberUpdate/Threshold2)")]
        [InlineData("10.0.14393", 2156, "Windows 10.0.14393.2156 (1607/AnniversaryUpdate/Redstone1)")]
        [InlineData("10.0.15063", 997, "Windows 10.0.15063.997 (1703/CreatorsUpdate/Redstone2)")]
        [InlineData("10.0.16299", 334, "Windows 10.0.16299.334 (1709/FallCreatorsUpdate/Redstone3)")]
        [InlineData("10.0.17134", 48, "Windows 10.0.17134.48 (1803/April2018Update/Redstone4)")]
        [InlineData("10.0.17763", 1, "Windows 10.0.17763.1 (1809/October2018Update/Redstone5)")]
        [InlineData("10.0.18362", 693, "Windows 10.0.18362.693 (1903/May2019Update/19H1)")]
        [InlineData("10.0.18363", 657, "Windows 10.0.18363.657 (1909/November2019Update/19H2)")]
        [InlineData("10.0.19041", 1, "Windows 10.0.19041.1 (2004/?/20H1)")]
        public void WindowsWithUbrIsPrettified(string originalVersion, int ubr, string prettifiedName)
            => Check(OsBrandStringHelper.Prettify("Windows", originalVersion, ubr), prettifiedName);

        [Theory]
        [InlineData("macOS 10.12.6 (16G29)", "Darwin 16.7.0", "macOS Sierra 10.12.6 (16G29) [Darwin 16.7.0]")]
        [InlineData("macOS 10.13.4 (17E199)", "Darwin 17.5.0", "macOS High Sierra 10.13.4 (17E199) [Darwin 17.5.0]")]
        [InlineData("macOS 10.15.4 (19E266)", "Darwin 19.4.0", "macOS Catalina 10.15.4 (19E266) [Darwin 19.4.0]")]
        public void MacOSXIsPrettified(string systemVersion, string kernelVersion, string prettifiedName)
            => Check(OsBrandStringHelper.PrettifyMacOSX(systemVersion, kernelVersion), prettifiedName);
    }
}