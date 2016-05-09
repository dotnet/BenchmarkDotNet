namespace BenchmarkDotNet.IntegrationTests.Classic
{
    public static class Program
    {
        // used only for easy debugging
        public static void Main(string[] args)
        {
            new JitOptimizationsTests().UserGetsNoWarningWhenOnlyOptimizedDllAreReferenced();
        }
    }
}
