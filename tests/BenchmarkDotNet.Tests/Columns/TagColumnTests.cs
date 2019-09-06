using BenchmarkDotNet.Columns;
using Xunit;

namespace BenchmarkDotNet.Tests.Columns
{
    public class TagColumnTests
    {
        [Fact]
        public void TagColumnsHasDifferentIds() // #1146
        {
            var column1 = new TagColumn("A", _ => _);
            var column2 = new TagColumn("B", _ => _);
            Assert.NotEqual(column1.Id, column2.Id);
        }
    }
}