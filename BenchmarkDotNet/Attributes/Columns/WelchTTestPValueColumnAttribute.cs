using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class WelchTTestPValueColumnAttribute : ColumnConfigAttribute
    {
        public WelchTTestPValueColumnAttribute() : base(BaselineScaledColumn.WelchTTestPValue)
        {
        }
    }
}