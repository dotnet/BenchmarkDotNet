using System;
using System.Text;
using BenchmarkDotNet.Horology;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Horology
{
    public class ChronometerTests
    {
        private readonly ITestOutputHelper output;

        public ChronometerTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void HardwareTimerKindTest()
        {
            var expected = new StringBuilder();
            var actual = new StringBuilder();
            var diff = new StringBuilder();
            Action<long, HardwareTimerKind> check = (freq, expectedKind) =>
            {
                var actualKind = Chronometer.GetHardwareTimerKind(freq);
                var message = actualKind == expectedKind ? "" : " [ERROR]";
                expected.AppendLine($"{freq}: {expectedKind}");
                actual.AppendLine($"{freq}: {actualKind}");
                diff.AppendLine($"{freq}: Expected = {expectedKind}; Actual = {actualKind}{message}");
            };

            check(64, HardwareTimerKind.System); // Common min frequency of GetSystemTimeAsFileTime
            check(1000, HardwareTimerKind.System); // Common work frequency of GetSystemTimeAsFileTime
            check(2000, HardwareTimerKind.System); // Common max frequency of GetSystemTimeAsFileTime            
            check(2143477, HardwareTimerKind.Tsc);
            check(2728067, HardwareTimerKind.Tsc);
            check(3507519, HardwareTimerKind.Tsc);
            check(3579545, HardwareTimerKind.Acpi);
            check(14318180, HardwareTimerKind.Hpet);
            check(10000000, HardwareTimerKind.Unknown); // Common value for Mono and VirtualBox

            output.WriteLine(diff.ToString());

            Assert.Equal(expected.ToString(), actual.ToString());
        }
    }
}