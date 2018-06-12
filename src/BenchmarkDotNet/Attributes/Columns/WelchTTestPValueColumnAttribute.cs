using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class WelchTTestPValueColumnAttribute : ColumnConfigBaseAttribute
    {
        public WelchTTestPValueColumnAttribute() : base(BaselineScaledColumn.WelchTTestPValue)
        {
        }
    }
}