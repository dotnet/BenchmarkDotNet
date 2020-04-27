using BenchmarkDotNet.Columns;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests
{
    public class SizeUnitTests
    {
        private readonly ITestOutputHelper output;

        public SizeUnitTests(ITestOutputHelper output)
        {
            this.output = output;
        }


        [Fact]
        public void ConvertTest()
        {
            CheckConvertTwoWay(1024, SizeUnit.B, 1, SizeUnit.KB);
            CheckConvertTwoWay(1024, SizeUnit.KB, 1, SizeUnit.MB);
            CheckConvertTwoWay(1024, SizeUnit.MB, 1, SizeUnit.GB);
            CheckConvertTwoWay(1024, SizeUnit.GB, 1, SizeUnit.TB);
            CheckConvertTwoWay(1024L * 1024 * 1024 * 1024, SizeUnit.B, 1, SizeUnit.TB);
        }

        [Theory]
        [InlineData("0 B", 0)]
        [InlineData("1 B", 1)]
        [InlineData("10 B", 10)]
        [InlineData("100 B", 100)]
        [InlineData("1000 B", 1000)]
        [InlineData("1023 B", 1023)]
        [InlineData("1 KB", 1024)]
        [InlineData("1 KB", 1025)]
        [InlineData("1.07 KB", 1100)]
        [InlineData("1.5 KB", 1024 + 512)]
        [InlineData("10 KB", 10 * 1024)]
        [InlineData("1023 KB", 1023 * 1024)]
        [InlineData("1 MB", 1024 * 1024)]
        [InlineData("1 GB", 1024 * 1024 * 1024)]
        [InlineData("1 TB", 1024L * 1024 * 1024 * 1024)]
        public void SizeUnitFormattingTest(string expected, long bytes)
        {
            Assert.Equal(expected, SizeValue.FromBytes(bytes).ToString(TestCultureInfo.Instance));
        }

        private void CheckConvertTwoWay(long value1, SizeUnit unit1, long value2, SizeUnit unit2)
        {
            CheckConvertOneWay(value1, unit1, value2, unit2);
            CheckConvertOneWay(value2, unit2, value1, unit1);
        }

        private void CheckConvertOneWay(long value1, SizeUnit unit1, long value2, SizeUnit unit2)
        {
            double convertedValue2 = SizeUnit.Convert(value1, unit1, unit2);
            output.WriteLine($"Expected: {value1} {unit1.Name} = {value2} {unit2.Name}");
            output.WriteLine($"Actual: {value1} {unit1.Name} = {convertedValue2} {unit2.Name}");
            output.WriteLine("");
            Assert.Equal(value2, convertedValue2, 4);
        }
    }
}