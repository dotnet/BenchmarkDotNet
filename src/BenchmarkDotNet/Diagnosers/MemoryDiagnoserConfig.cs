using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
    public class MemoryDiagnoserConfig
    {
        /// <param name="displayGenColumns">Display Garbage Collections per Generation columns (Gen 0, Gen 1, Gen 2). True by default.</param>
        /// <param name="includeSurvived">If true, monitoring will be enabled and survived memory will be measured on the first benchmark run.</param>
        [PublicAPI]
        public MemoryDiagnoserConfig(bool displayGenColumns = true, bool includeSurvived = false)
        {
            DisplayGenColumns = displayGenColumns;
            IncludeSurvived = includeSurvived;
        }

        public bool DisplayGenColumns { get; }
        public bool IncludeSurvived { get; }
    }
}