using System.Globalization;

namespace BenchmarkDotNet
{
    public class BenchmarkSettings
    {
        private static readonly BenchmarkSettings instance = new BenchmarkSettings();

        public static BenchmarkSettings Instance
        {
            get { return instance; }
        }

        public BenchmarkSettings()
        {
            DetailedMode = false;
            CultureInfo = new CultureInfo("") { NumberFormat = { NumberDecimalSeparator = "." } };

            DefaultResultIterationCount = 10;
            DefaultMaxWarmUpIterationCount = 30;
            DefaultWarmUpIterationCount = 5;
            DefaultMaxWarmUpError = 0.05;
            DefaultPrintBenchmarkBodyToConsole = true;
            DefaultProcessorAffinity = 2;
        }

        public bool DetailedMode { get; set; }
        public CultureInfo CultureInfo { get; set; }

        public int DefaultResultIterationCount { get; set; }
        public int DefaultMaxWarmUpIterationCount { get; set; }
        public int DefaultWarmUpIterationCount { get; set; }
        public double DefaultMaxWarmUpError { get; set; }
        public bool DefaultPrintBenchmarkBodyToConsole { get; set; }
        public int DefaultProcessorAffinity { get; set; }
    }
}