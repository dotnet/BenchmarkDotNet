namespace BenchmarkDotNet.Columns
{
    public interface IColumnHidingRule
    {
        bool NeedToHide(IColumn column);
    }
}