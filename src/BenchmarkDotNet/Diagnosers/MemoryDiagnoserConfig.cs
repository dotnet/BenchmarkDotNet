using JetBrains.Annotations;

namespace BenchmarkDotNet.Diagnosers
{
    public class MemoryDiagnoserConfig
    {
        /// <param name="displayGenColumns">Display Garbage Collections per Generation columns (Gen 0, Gen 1, Gen 2). True by default.</param>
        [PublicAPI]
        public MemoryDiagnoserConfig(bool displayGenColumns = true)
        {
            DisplayGenColumns = displayGenColumns;
        }

        public bool DisplayGenColumns { get; }
    }
}