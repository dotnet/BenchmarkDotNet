using System.Text;
using BenchmarkDotNet.Horology;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Horology
{
    public class ChronometerTests
    {
        private readonly ITestOutputHelper output;

        public ChronometerTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void HardwareTimerKindTest()
        {
            var expected = new StringBuilder();
            var actual = new StringBuilder();
            var diff = new StringBuilder();

            void Check(long freq, HardwareTimerKind expectedKind)
            {
                var actualKind = Chronometer.GetHardwareTimerKind(freq);
                string message = actualKind == expectedKind ? "" : " [ERROR]";
                expected.AppendLine($"{freq}: {expectedKind}");
                actual.AppendLine($"{freq}: {actualKind}");
                diff.AppendLine($"{freq}: Expected = {expectedKind}; Actual = {actualKind}{message}");
            }

            Check(64, HardwareTimerKind.System); // Common min frequency of GetSystemTimeAsFileTime
            Check(1000, HardwareTimerKind.System); // Common work frequency of GetSystemTimeAsFileTime
            Check(2000, HardwareTimerKind.System); // Common max frequency of GetSystemTimeAsFileTime
            Check(2143477, HardwareTimerKind.Tsc);
            Check(2728067, HardwareTimerKind.Tsc);
            Check(3507519, HardwareTimerKind.Tsc);
            Check(3579545, HardwareTimerKind.Acpi);
            Check(14318180, HardwareTimerKind.Hpet);
            Check(10000000, HardwareTimerKind.Unknown); // Common value for Mono and VirtualBox

            output.WriteLine(diff.ToString());

            Assert.Equal(expected.ToString(), actual.ToString());
        }
    }
}