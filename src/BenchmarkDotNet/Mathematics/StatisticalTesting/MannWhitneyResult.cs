namespace BenchmarkDotNet.Mathematics.StatisticalTesting
{
    public class MannWhitneyResult : OneSidedTestResult
    {
        public double Ux { get; }
        public double Uy { get; }

        public MannWhitneyResult(double ux, double uy, double pValue, Threshold threshold) : base(pValue, threshold)
        {
            Ux = ux;
            Uy = uy;
        }

        public override string ToString() => $"{nameof(Ux)}: {Ux}, {nameof(Uy)}: {Uy}, {nameof(PValue)}: {PValue}";
    }
}