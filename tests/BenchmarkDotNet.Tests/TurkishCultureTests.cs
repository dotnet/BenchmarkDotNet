using System.Globalization;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Tests.Builders;
using BenchmarkDotNet.Validators;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Tests
{
    // In the Turkish culture, "I".ToLower() is "ı" (dotless) and "i".ToUpper() is "İ" (dotted),
    // so names and identifiers must be cased with the invariant culture.
    public class TurkishCultureTests : IDisposable
    {
        private readonly CultureInfo initCulture;

        public TurkishCultureTests()
        {
            initCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
        }

        public void Dispose() => Thread.CurrentThread.CurrentCulture = initCulture;

        [Fact]
        public void HardwareTimerKindIsUpperCasedWithTheInvariantCulture()
        {
            var info = new HostEnvironmentInfoBuilder()
                .WithHardwareTimerKind(HardwareTimerKind.Acpi)
                .Build();

            Assert.Contains(info.ToFormattedString(), line => line.EndsWith("Timer: ACPI"));
        }

        [Fact]
        public void CsharpKeywordsAreLowerCasedWithTheInvariantCulture()
        {
            var keywords = CompilationValidator.GetCsharpKeywords().ToArray();

            Assert.Contains("if", keywords);
            Assert.Contains("int", keywords);
            Assert.Contains("interface", keywords);
            Assert.DoesNotContain(keywords, keyword => keyword.Contains('ı'));
        }
    }
}
