using System.Linq;
using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkMeasurementStatistic
    {
        public double Min { get; }
        public double Max { get; }
        public double Median { get; }
        public double StandardDeviation { get; }
        public double Error { get; }

        public BenchmarkMeasurementStatistic(double[] values)
        {
            if (values.Length == 0)
            {
                Min = Max = Median = 00;
                StandardDeviation = Error = double.PositiveInfinity;
            }
            else
            {
                Min = values.Min();
                Max = values.Max();
                Median = values.Median();
                StandardDeviation = values.StandardDeviation();
                Error = (Max - Min) * 1.0 / Min;
            }
        }

        public override string ToString()
        {
            return string.Format(
                EnvironmentHelper.MainCultureInfo,
                "Median = {0:0.#####}; StdDev = {1:0.#####}; Min = {2:0.#####}; Max = {3:0.#####};",
                Median, StandardDeviation, Min, Max);
        }
    }
}