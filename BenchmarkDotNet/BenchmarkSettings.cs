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
        }

        public bool DetailedMode { get; set; }
    }
}