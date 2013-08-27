namespace BenchmarkDotNet
{
    public class BenchmarkInfo
    {
        public BenchmarkRunList WarmUp { get; set; }
        public BenchmarkRunList Result { get; set; }

        public void Print()
        {
            ConsoleHelper.WriteLineHeader("WarmUp:");
            WarmUp.Print();
            ConsoleHelper.NewLine();
            ConsoleHelper.WriteLineHeader("Result:");
            Result.Print();
        }
    }
}