using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class WelchTTestPValueColumnAttribute : ColumnConfigBaseAttribute
    {
        public WelchTTestPValueColumnAttribute() : base(BaselineScaledColumn.WelchTTestPValue)
        {
        }
    }
}