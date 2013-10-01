namespace BenchmarkDotNet
{
    public static class BenchmarkUtils
    {
        public static string CultureFormat(string format, params object[] args)
        {
            return string.Format(BenchmarkSettings.Instance.CultureInfo, format, args);
        }
    }
}